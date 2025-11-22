using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.RegularExpressions;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("ai")]
public class InsightsController : ControllerBase
{
    private readonly ReviewPredictorService _reviewPredictor;
    private readonly IApprovalPredictor _approvalPredictor;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(ReviewPredictorService reviewPredictor, IApprovalPredictor approvalPredictor, ILogger<InsightsController> logger)
    {
        _reviewPredictor = reviewPredictor;
        _approvalPredictor = approvalPredictor;
        _logger = logger;
    }

    [HttpGet("clients/{clientId}/summary")]
    public async Task<ActionResult<ClientSummaryResponse>> GetClientSummary(Guid clientId, CancellationToken ct)
    {
        var summary = await _reviewPredictor.GetOrRefreshSummaryAsync(clientId, ct);
        if (summary == null)
        {
            return NotFound(new { message = "Client not found or no data." });
        }
        // Strip counts signature token
        var cleaned = Regex.Replace(summary.SummaryText, @"\n\[counts:[0-9]+:[0-9]+\]$", string.Empty).Trim();
        return Ok(new ClientSummaryResponse
        {
            ClientId = clientId,
            Summary = cleaned,
            GeneratedAt = summary.UpdatedAt
        });
    }

    [HttpPost("predict")]
    public async Task<ActionResult<ApprovalPredictionResponse>> PredictApproval([FromBody] ApprovalPredictionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var (prob, rationale) = await _approvalPredictor.PredictApprovalAsync(request.ClientId, request.SubmissionId, ct);
        return Ok(new ApprovalPredictionResponse
        {
            ClientId = request.ClientId,
            SubmissionId = request.SubmissionId,
            Probability = prob,
            Rationale = rationale
        });
    }
}
