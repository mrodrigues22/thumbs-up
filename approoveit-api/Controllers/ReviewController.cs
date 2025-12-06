using Microsoft.AspNetCore.Mvc;
using ApprooveItApi.DTOs;
using ApprooveItApi.Models;
using ApprooveItApi.Services;
using ApprooveItApi.Repositories;
using ApprooveItApi.Mappers;

namespace ApprooveItApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReviewController> _logger;
    private readonly SubmissionMapper _submissionMapper;
    
    public ReviewController(
        ISubmissionRepository submissionRepository,
        IReviewRepository reviewRepository,
        IEmailService emailService,
        ILogger<ReviewController> logger,
        SubmissionMapper submissionMapper)
    {
        _submissionRepository = submissionRepository;
        _reviewRepository = reviewRepository;
        _emailService = emailService;
        _logger = logger;
        _submissionMapper = submissionMapper;
    }
    
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateAccess([FromBody] ValidateAccessRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var submission = await _submissionRepository.GetByTokenAsync(request.AccessToken);
        
        if (submission == null)
        {
            return NotFound(new { message = "Invalid access token" });
        }
        
        // Check expiration
        if (submission.ExpiresAt < DateTime.UtcNow)
        {
            submission.Status = SubmissionStatus.Expired;
            await _submissionRepository.UpdateAsync(submission);
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
        
        // First get submission to find the user ID
        var submissionBasic = await _submissionRepository.GetByTokenAsync(token);
        if (submissionBasic == null)
        {
            return NotFound(new { message = "Submission not found" });
        }
        
        // Now get with includes using the user ID
        var submission = await _submissionRepository.GetByIdWithIncludesAsync(submissionBasic.Id, submissionBasic.CreatedById);
        if (submission == null)
        {
            return NotFound(new { message = "Submission not found" });
        }
        
        // Check expiration
        if (submission.ExpiresAt < DateTime.UtcNow)
        {
            submission.Status = SubmissionStatus.Expired;
            await _submissionRepository.UpdateAsync(submission);
            return BadRequest(new { message = "This link has expired" });
        }
        
        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, submission.AccessPasswordHash))
        {
            return Unauthorized(new { message = "Invalid access password" });
        }
        
        return Ok(_submissionMapper.ToResponse(submission));
    }
    
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        // First get submission to find the user ID
        var submissionBasic = await _submissionRepository.GetByTokenAsync(request.AccessToken);
        if (submissionBasic == null)
        {
            return NotFound(new { message = "Submission not found" });
        }
        
        // Now get with includes using the user ID
        var submission = await _submissionRepository.GetByIdWithIncludesAsync(submissionBasic.Id, submissionBasic.CreatedById);
        if (submission == null)
        {
            return NotFound(new { message = "Submission not found" });
        }
        
        // Check expiration
        if (submission.ExpiresAt < DateTime.UtcNow)
        {
            submission.Status = SubmissionStatus.Expired;
            await _submissionRepository.UpdateAsync(submission);
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
        
        // Validate that comment is provided when rejecting
        if (request.Status == ReviewStatus.Rejected && string.IsNullOrWhiteSpace(request.Comment))
        {
            return BadRequest(new { message = "A comment is required when rejecting a submission" });
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
        
        await _reviewRepository.CreateAsync(review);
        await _submissionRepository.UpdateAsync(submission);
        
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
