using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("ai")]
public class InsightsController : ControllerBase
{
    private readonly IReviewPredictorService _reviewPredictor;
    private readonly IApprovalPredictor _approvalPredictor;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(
        IReviewPredictorService reviewPredictor, 
        IApprovalPredictor approvalPredictor,
        ISubscriptionService subscriptionService,
        ILogger<InsightsController> logger)
    {
        _reviewPredictor = reviewPredictor;
        _approvalPredictor = approvalPredictor;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("clients/{clientId}/summary")]
    public async Task<ActionResult<ClientSummaryResponse>> GetClientSummary(Guid clientId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        // Check AI feature access
        var hasAccess = await _subscriptionService.CheckFeatureAccessAsync(userId, "ai_features");
        if (!hasAccess)
        {
            return StatusCode(402, new 
            { 
                message = "AI features require a Pro or Enterprise subscription.",
                feature = "ai_features",
                upgradeUrl = "/subscription"
            });
        }
        
        try
        {
            var summary = await _reviewPredictor.GetClientSummaryAsync(clientId, userId, ct);
            if (summary == null)
            {
                return NotFound(new { message = "Client not found or no data." });
            }
            
            return Ok(summary);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            _logger.LogWarning(ex, "AI service not configured for client summary");
            return StatusCode(503, new { message = "AI service is not configured. Please contact administrator." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client summary for {ClientId}", clientId);
            return StatusCode(500, new { message = "Failed to generate summary. Please try again later." });
        }
    }
    


    [HttpPost("predict")]
    public async Task<ActionResult<ApprovalPredictionResponse>> PredictApproval([FromBody] ApprovalPredictionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        // Check AI feature access
        var hasAccess = await _subscriptionService.CheckFeatureAccessAsync(userId, "ai_features");
        if (!hasAccess)
        {
            return StatusCode(402, new 
            { 
                message = "AI features require a Pro or Enterprise subscription.",
                feature = "ai_features",
                upgradeUrl = "/subscription"
            });
        }
        
        try
        {
            var prediction = await _approvalPredictor.PredictApprovalAsync(request.ClientId, request.SubmissionId, userId, ct);
            return Ok(prediction);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            _logger.LogWarning(ex, "AI service not configured for approval prediction");
            return StatusCode(503, new { message = "AI service is not configured. Please contact administrator." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting approval for submission {SubmissionId}", request.SubmissionId);
            return StatusCode(500, new { message = "Failed to predict approval. Please try again later." });
        }
    }
}
