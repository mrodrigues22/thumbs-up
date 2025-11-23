using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
        var summary = await _reviewPredictor.GetOrRefreshSummaryAsync(clientId, userId, ct);
        if (summary == null)
        {
            return NotFound(new { message = "Client not found or no data." });
        }
        
        // Strip counts signature token and parse counts
        var cleaned = Regex.Replace(summary.SummaryText, @"\n\[counts:[0-9]+:[0-9]+\]$", string.Empty).Trim();
        var countsMatch = Regex.Match(summary.SummaryText, @"\[counts:([0-9]+):([0-9]+)\]$");
        var approved = countsMatch.Success ? int.Parse(countsMatch.Groups[1].Value) : 0;
        var rejected = countsMatch.Success ? int.Parse(countsMatch.Groups[2].Value) : 0;
        
        // Parse sections from the summary text
        var (stylePrefs, positives, rejections) = ParseSummarySections(cleaned);
        
        return Ok(new ClientSummaryResponse
        {
            ClientId = clientId,
            StylePreferences = stylePrefs,
            RecurringPositives = positives,
            RejectionReasons = rejections,
            ApprovedCount = approved,
            RejectedCount = rejected,
            GeneratedAt = summary.UpdatedAt
        });
    }
    
    private (List<string> style, List<string> positives, List<string> rejections) ParseSummarySections(string summaryText)
    {
        var style = new List<string>();
        var positives = new List<string>();
        var rejections = new List<string>();
        
        if (string.IsNullOrWhiteSpace(summaryText))
            return (style, positives, rejections);
        
        var lines = summaryText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var currentSection = "";
        
        foreach (var line in lines)
        {
            var lower = line.ToLowerInvariant();
            
            // Detect section headers
            if (lower.Contains("style") && (lower.Contains("preference") || lower.Contains(":") || lower.Contains("tendencies")))
            {
                currentSection = "style";
                continue;
            }
            else if (lower.Contains("positive") || lower.Contains("strength") || lower.Contains("success"))
            {
                currentSection = "positives";
                continue;
            }
            else if (lower.Contains("rejection") || lower.Contains("issue") || lower.Contains("concern"))
            {
                currentSection = "rejections";
                continue;
            }
            
            // Parse bullet points
            var cleaned = line.TrimStart('-', '•', '*', '·').Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
                continue;
            
            // Add to appropriate section
            if (currentSection == "style" && !cleaned.EndsWith(':'))
                style.Add(cleaned);
            else if (currentSection == "positives" && !cleaned.EndsWith(':'))
                positives.Add(cleaned);
            else if (currentSection == "rejections" && !cleaned.EndsWith(':'))
                rejections.Add(cleaned);
        }
        
        return (style, positives, rejections);
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
