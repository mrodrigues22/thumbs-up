using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;
using ThumbsUpApi.Repositories;
using ThumbsUpApi.Mappers;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Helpers;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubmissionController> _logger;
    private readonly SubmissionMapper _mapper;
    private readonly ThumbsUpApi.Services.ISubmissionAnalysisQueue _analysisQueue;
    private readonly IContentSummaryService _contentSummaryService;
    
    public SubmissionController(
        ISubmissionRepository submissionRepository,
        IClientRepository clientRepository,
        IFileStorageService fileStorage,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SubmissionController> logger,
        SubmissionMapper mapper,
        ThumbsUpApi.Services.ISubmissionAnalysisQueue analysisQueue,
        IContentSummaryService contentSummaryService)
    {
        _submissionRepository = submissionRepository;
        _clientRepository = clientRepository;
        _fileStorage = fileStorage;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _mapper = mapper;
        _analysisQueue = analysisQueue;
        _contentSummaryService = contentSummaryService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateSubmission([FromForm] CreateSubmissionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        // Validate that either ClientId or ClientEmail is provided
        if (!request.ClientId.HasValue && string.IsNullOrWhiteSpace(request.ClientEmail))
        {
            return BadRequest(new { message = "Either ClientId or ClientEmail is required" });
        }
        
        // Validate files
        if (request.Files == null || request.Files.Count == 0)
            return BadRequest(new { message = "At least one file is required" });
        
        // Check all files are same type
        var firstFileIsImage = IsImageFile(request.Files[0]);
        if (request.Files.Any(f => IsImageFile(f) != firstFileIsImage))
        {
            return BadRequest(new { message = "All files must be of the same type (all images or all videos)" });
        }
        
        // Check file sizes
        var maxFileSize = _configuration.GetValue<long>("Submission:MaxFileSize", 104857600); // 100MB default
        if (request.Files.Any(f => f.Length > maxFileSize))
        {
            return BadRequest(new { message = $"File size must not exceed {maxFileSize / 1048576}MB" });
        }
        
        // Handle client: use existing or create new
        Client? client = null;
        string clientEmail;
        
        if (request.ClientId.HasValue)
        {
            // Option 1: Use existing client
            client = await _clientRepository.GetByIdAsync(request.ClientId.Value, userId);
            if (client == null)
            {
                return BadRequest(new { message = "Client not found" });
            }
            
            // Verify ownership (defense in depth)
            if (client.CreatedById != userId)
            {
                return BadRequest(new { message = "Client not found" });
            }
            
            clientEmail = client.Email;
            
            // Update LastUsedAt
            client.LastUsedAt = DateTime.UtcNow;
            await _clientRepository.UpdateAsync(client);
        }
        else
        {
            clientEmail = request.ClientEmail!;
            
            // Option 2: Check if we should create a new client (if ClientName is provided)
            if (!string.IsNullOrWhiteSpace(request.ClientName))
            {
                // Check if client already exists
                var existingClient = await _clientRepository.FindByEmailAsync(clientEmail, userId);
                if (existingClient != null)
                {
                    client = existingClient;
                    client.LastUsedAt = DateTime.UtcNow;
                    await _clientRepository.UpdateAsync(client);
                }
                else
                {
                    // Create new client
                    client = new Client
                    {
                        Id = Guid.NewGuid(),
                        CreatedById = userId,
                        Email = clientEmail,
                        Name = request.ClientName,
                        CreatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow
                    };
                    await _clientRepository.CreateAsync(client);
                }
            }
            // Option 3: Quick email entry (no ClientName) - don't create client
        }
        
        // Generate secure password
        var accessPassword = GenerateAccessPassword();
        
        // Create submission
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            CreatedById = userId,
            ClientId = client?.Id,
            ClientEmail = clientEmail,
            AccessToken = GenerateAccessToken(),
            AccessPasswordHash = BCrypt.Net.BCrypt.HashPassword(accessPassword),
            AccessPassword = accessPassword,
            Message = request.Message,
            Captions = request.Captions,
            Status = SubmissionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Submission:LinkExpirationDays", 7))
        };
        
        // Upload files
        var mediaFiles = new List<MediaFile>();
        foreach (var file in request.Files)
        {
            try
            {
                var filePath = await _fileStorage.UploadAsync(file, submission.Id.ToString());
                
                var mediaFile = new MediaFile
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileType = IsImageFile(file) ? MediaFileType.Image : MediaFileType.Video,
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow
                };
                
                mediaFiles.Add(mediaFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                return StatusCode(500, new { message = "Error uploading files" });
            }
        }
        
        submission.MediaFiles = mediaFiles;
        
        await _submissionRepository.CreateAsync(submission);
        // Enqueue AI analysis (images) in background
        _analysisQueue.Enqueue(submission.Id);
        
        // Send email to client
        var reviewLink = $"{Request.Scheme}://{Request.Host}/review/{submission.AccessToken}";
        await _emailService.SendReviewLinkAsync(
            submission.ClientEmail,
            reviewLink,
            accessPassword,
            submission.Message
        );
        
        _logger.LogInformation("Submission {SubmissionId} created by user {UserId}", submission.Id, userId);
        
        var response = _mapper.ToResponse(submission);
        response.AccessPassword = accessPassword;
        response.ContentSummary = await _contentSummaryService.GenerateAsync(response);
        
        return Ok(response);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetSubmissions([FromQuery] SubmissionQueryObject query)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var submissions = await _submissionRepository.GetAllByUserIdAsync(userId, query);
        
        return Ok(submissions.Select(s => _mapper.ToResponse(s)));
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubmission(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var submission = await _submissionRepository.GetByIdWithIncludesAsync(id, userId);
        
        if (submission == null)
            return NotFound();
        var response = _mapper.ToResponse(submission);
        response.ContentSummary = await _contentSummaryService.GenerateAsync(response);
        return Ok(response);
    }

    [HttpPost("{id}/reanalyze")]
    public async Task<IActionResult> ReanalyzeSubmission(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var submission = await _submissionRepository.GetByIdAsync(id, userId);
        if (submission == null)
        {
            return NotFound();
        }

        _analysisQueue.Enqueue(id);
        _logger.LogInformation("Reanalysis queued for submission {SubmissionId} by user {UserId}", id, userId);
        return Accepted(new { message = "Creative insights refresh queued" });
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubmission(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var submission = await _submissionRepository.GetByIdWithIncludesAsync(id, userId);
        
        if (submission == null)
            return NotFound();
        
        // Delete files from storage
        foreach (var file in submission.MediaFiles)
        {
            await _fileStorage.DeleteAsync(file.FilePath);
        }
        
        await _submissionRepository.DeleteAsync(submission);
        
        _logger.LogInformation("Submission {SubmissionId} deleted by user {UserId}", id, userId);
        
        return NoContent();
    }
    
    private bool IsImageFile(IFormFile file)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return imageExtensions.Contains(extension);
    }
    
    private string GenerateAccessToken()
    {
        return Guid.NewGuid().ToString("N");
    }
    
    private string GenerateAccessPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

}
