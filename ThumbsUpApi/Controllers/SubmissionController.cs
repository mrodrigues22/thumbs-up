using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;
using ThumbsUpApi.Repositories;
using ThumbsUpApi.Mappers;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubmissionController> _logger;
    private readonly SubmissionMapper _mapper;
    
    public SubmissionController(
        ISubmissionRepository submissionRepository,
        IFileStorageService fileStorage,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SubmissionController> logger,
        SubmissionMapper mapper)
    {
        _submissionRepository = submissionRepository;
        _fileStorage = fileStorage;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _mapper = mapper;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateSubmission([FromForm] CreateSubmissionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
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
        
        // Create submission
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            CreatedById = userId,
            ClientEmail = request.ClientEmail,
            AccessToken = GenerateAccessToken(),
            AccessPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AccessPassword),
            Message = request.Message,
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
        
        // Send email to client
        var reviewLink = $"{Request.Scheme}://{Request.Host}/review/{submission.AccessToken}";
        await _emailService.SendReviewLinkAsync(
            submission.ClientEmail,
            reviewLink,
            request.AccessPassword,
            submission.Message
        );
        
        _logger.LogInformation("Submission {SubmissionId} created by user {UserId}", submission.Id, userId);
        
        return Ok(_mapper.ToResponse(submission));
    }
    
    [HttpGet]
    public async Task<IActionResult> GetSubmissions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var submissions = await _submissionRepository.GetAllByUserIdAsync(userId);
        
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
        
        return Ok(_mapper.ToResponse(submission));
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
}
