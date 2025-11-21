using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;
using ThumbsUpApi.Mappers;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly SubmissionMapper _submissionMapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageCompressionService _imageCompressionService;
    private readonly ILogger<ClientController> _logger;

    public ClientController(
        IClientRepository clientRepository,
        ISubmissionRepository submissionRepository,
        SubmissionMapper submissionMapper,
        IFileStorageService fileStorageService,
        IImageCompressionService imageCompressionService,
        ILogger<ClientController> logger)
    {
        _clientRepository = clientRepository;
        _submissionRepository = submissionRepository;
        _submissionMapper = submissionMapper;
        _fileStorageService = fileStorageService;
        _imageCompressionService = imageCompressionService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientResponse>>> GetClients()
    {
        var userId = GetUserId();
        var clients = await _clientRepository.ListByUserAsync(userId);
        
        var clientResponses = new List<ClientResponse>();
        foreach (var client in clients)
        {
            var submissionCount = await _clientRepository.GetSubmissionCountAsync(client.Id);
            clientResponses.Add(new ClientResponse
            {
                Id = client.Id,
                Email = client.Email,
                Name = client.Name,
                CompanyName = client.CompanyName,
                ProfilePictureUrl = client.ProfilePictureUrl,
                CreatedAt = client.CreatedAt,
                LastUsedAt = client.LastUsedAt,
                SubmissionCount = submissionCount
            });
        }
        
        return Ok(clientResponses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientResponse>> GetClient(Guid id)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        var submissionCount = await _clientRepository.GetSubmissionCountAsync(client.Id);
        
        return Ok(new ClientResponse
        {
            Id = client.Id,
            Email = client.Email,
            Name = client.Name,
            CompanyName = client.CompanyName,
            ProfilePictureUrl = client.ProfilePictureUrl,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            SubmissionCount = submissionCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> CreateClient([FromBody] CreateClientRequest request)
    {
        var userId = GetUserId();
        
        // Check if client with this email already exists for this user
        var existingClient = await _clientRepository.FindByEmailAsync(request.Email, userId);
        if (existingClient != null)
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }
        
        var client = new Client
        {
            Id = Guid.NewGuid(),
            CreatedById = userId,
            Email = request.Email,
            Name = request.Name,
            CompanyName = request.CompanyName,
            CreatedAt = DateTime.UtcNow
        };
        
        await _clientRepository.CreateAsync(client);
        
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, new ClientResponse
        {
            Id = client.Id,
            Email = client.Email,
            Name = client.Name,
            CompanyName = client.CompanyName,
            ProfilePictureUrl = client.ProfilePictureUrl,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            SubmissionCount = 0
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClientResponse>> UpdateClient(Guid id, [FromBody] UpdateClientRequest request)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        // Check if another client with this email already exists for this user
        var existingClient = await _clientRepository.FindByEmailAsync(request.Email, userId);
        if (existingClient != null && existingClient.Id != id)
        {
            return BadRequest(new { message = "A client with this email already exists" });
        }
        
        client.Email = request.Email;
        client.Name = request.Name;
        client.CompanyName = request.CompanyName;
        
        await _clientRepository.UpdateAsync(client);
        
        var submissionCount = await _clientRepository.GetSubmissionCountAsync(client.Id);
        
        return Ok(new ClientResponse
        {
            Id = client.Id,
            Email = client.Email,
            Name = client.Name,
            CompanyName = client.CompanyName,
            ProfilePictureUrl = client.ProfilePictureUrl,
            CreatedAt = client.CreatedAt,
            LastUsedAt = client.LastUsedAt,
            SubmissionCount = submissionCount
        });
    }

    [HttpGet("{id}/submissions")]
    public async Task<ActionResult<IEnumerable<SubmissionResponse>>> GetClientSubmissions(Guid id)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        var submissions = await _submissionRepository.GetByClientIdAsync(id, userId);
        
        return Ok(submissions.Select(s => _submissionMapper.ToResponse(s)));
    }

    [HttpPost("{id}/picture")]
    public async Task<IActionResult> UploadClientProfilePicture(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { message = "Invalid file type. Only images are allowed." });

        // Validate file size (max 10MB before compression)
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds 10MB limit" });

        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }

        try
        {
            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(client.ProfilePictureUrl))
            {
                await _fileStorageService.DeleteAsync(client.ProfilePictureUrl);
            }

            // Compress the image to WebP format (800x800 max, 85% quality)
            using var originalStream = file.OpenReadStream();
            using var compressedStream = await _imageCompressionService.CompressImageAsync(originalStream, 800, 800, 85);

            // Generate unique filename with .webp extension
            var uniqueFileName = $"{Guid.NewGuid()}.webp";

            // Upload compressed image
            var filePath = await _fileStorageService.UploadFromStreamAsync(compressedStream, uniqueFileName, "client-pictures");
            var fileUrl = _fileStorageService.GetFileUrl(filePath);

            // Update client's profile picture URL
            client.ProfilePictureUrl = fileUrl;
            await _clientRepository.UpdateAsync(client);

            _logger.LogInformation("Client {ClientId} profile picture updated (compressed to WebP)", id);

            return Ok(new { profilePictureUrl = fileUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing profile picture for client {ClientId}", id);
            return StatusCode(500, new { message = "Failed to process image" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var userId = GetUserId();
        var client = await _clientRepository.GetByIdAsync(id, userId);
        
        if (client == null)
        {
            return NotFound(new { message = "Client not found" });
        }
        
        // Check if client has submissions
        var submissionCount = await _clientRepository.GetSubmissionCountAsync(id);
        if (submissionCount > 0)
        {
            return BadRequest(new { message = $"Cannot delete client with {submissionCount} submission(s). Please delete submissions first." });
        }
        
        await _clientRepository.DeleteAsync(id);
        
        return NoContent();
    }
}
