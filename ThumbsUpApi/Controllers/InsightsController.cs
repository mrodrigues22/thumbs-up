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
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(IReviewPredictorService reviewPredictor, IApprovalPredictor approvalPredictor, ILogger<InsightsController> logger)
    {
        _reviewPredictor = reviewPredictor;
        _approvalPredictor = approvalPredictor;
        _logger = logger;
    }

    [HttpGet("clients/{clientId}/summary")]
    public async Task<ActionResult<ClientSummaryResponse>> GetClientSummary(Guid clientId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var summary = await _reviewPredictor.GetClientSummaryAsync(clientId, userId, ct);
        if (summary == null)
        {
            return NotFound(new { message = "Client not found or no data." });
        }
        
        return Ok(summary);
    }
    


    [HttpPost("predict")]
    public async Task<ActionResult<ApprovalPredictionResponse>> PredictApproval([FromBody] ApprovalPredictionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (prob, rationale) = await _approvalPredictor.PredictApprovalAsync(request.ClientId, request.SubmissionId, userId, ct);
        return Ok(new ApprovalPredictionResponse
        {
            ClientId = request.ClientId,
            SubmissionId = request.SubmissionId,
            Probability = prob,
            Rationale = rationale
        });
    }
}
