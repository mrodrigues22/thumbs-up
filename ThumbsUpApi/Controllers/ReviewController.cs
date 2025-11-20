using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReviewController> _logger;
    
    public ReviewController(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IEmailService emailService,
        ILogger<ReviewController> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _emailService = emailService;
        _logger = logger;
    }
    
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateAccess([FromBody] ValidateAccessRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var submission = await _context.Submissions
            .FirstOrDefaultAsync(s => s.AccessToken == request.AccessToken);
        
        if (submission == null)
        {
            return NotFound(new { message = "Invalid access token" });
        }
        
        // Check expiration
        if (submission.ExpiresAt < DateTime.UtcNow)
        {
            submission.Status = SubmissionStatus.Expired;
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "This link has expired" });
        }
        
        // Check if already reviewed
        if (submission.Status != SubmissionStatus.Pending)
        {
            return BadRequest(new { message = "This submission has already been reviewed" });
        }
        
        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.AccessPassword, submission.AccessPasswordHash))
        {
            return Unauthorized(new { message = "Invalid access password" });
        }
        
        return Ok(new { message = "Access granted", valid = true });
    }
    
    [HttpGet("{token}")]
    public async Task<IActionResult> GetSubmissionByToken(string token, [FromQuery] string password)
    {
        if (string.IsNullOrEmpty(password))
            return BadRequest(new { message = "Password is required" });
        
        var submission = await _context.Submissions
            .Include(s => s.MediaFiles)
            .Include(s => s.Review)
            .FirstOrDefaultAsync(s => s.AccessToken == token);
        
        if (submission == null)
        {
            return NotFound(new { message = "Submission not found" });
        }
        
        // Check expiration
        if (submission.ExpiresAt < DateTime.UtcNow)
        {
            submission.Status = SubmissionStatus.Expired;
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "This link has expired" });
        }
        
        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, submission.AccessPasswordHash))
        {
            return Unauthorized(new { message = "Invalid access password" });
        }
        
        var response = new SubmissionResponse
        {
            Id = submission.Id,
            ClientEmail = submission.ClientEmail,
            AccessToken = submission.AccessToken,
            Message = submission.Message,
            Status = submission.Status,
            CreatedAt = submission.CreatedAt,
            ExpiresAt = submission.ExpiresAt,
            MediaFiles = submission.MediaFiles.Select(m => new MediaFileResponse
            {
                Id = m.Id,
                FileName = m.FileName,
                FileUrl = _fileStorage.GetFileUrl(m.FilePath),
                FileType = m.FileType,
                FileSize = m.FileSize,
                UploadedAt = m.UploadedAt
            }).ToList(),
            Review = submission.Review != null ? new ReviewResponse
            {
                Id = submission.Review.Id,
                Status = submission.Review.Status,
                Comment = submission.Review.Comment,
                ReviewedAt = submission.Review.ReviewedAt
            } : null
        };
        
        return Ok(response);
    }
    
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var submission = await _context.Submissions
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.AccessToken == request.AccessToken);
        
        if (submission == null)
        {
            return NotFound(new { message = "Submission not found" });
        }
        
        // Check expiration
        if (submission.ExpiresAt < DateTime.UtcNow)
        {
            submission.Status = SubmissionStatus.Expired;
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "This link has expired" });
        }
        
        // Check if already reviewed
        if (submission.Status != SubmissionStatus.Pending)
        {
            return BadRequest(new { message = "This submission has already been reviewed" });
        }
        
        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.AccessPassword, submission.AccessPasswordHash))
        {
            return Unauthorized(new { message = "Invalid access password" });
        }
        
        // Create review
        var review = new Review
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            Status = request.Status,
            Comment = request.Comment,
            ReviewedAt = DateTime.UtcNow
        };
        
        // Update submission status
        submission.Status = request.Status == ReviewStatus.Approved
            ? SubmissionStatus.Approved
            : SubmissionStatus.Rejected;
        
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        
        // Send notification to professional
        await _emailService.SendReviewNotificationAsync(
            submission.CreatedBy.Email!,
            submission.Id.ToString(),
            request.Status == ReviewStatus.Approved
        );
        
        _logger.LogInformation(
            "Review submitted for submission {SubmissionId} with status {Status}",
            submission.Id,
            request.Status);
        
        return Ok(new { message = "Review submitted successfully" });
    }
}
