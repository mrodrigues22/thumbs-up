using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;

namespace ThumbsUpApi.Services;

public class HybridApprovalPredictor : IApprovalPredictor
{
    private readonly ApplicationDbContext _db;
    private readonly IContentFeatureRepository _featureRepo;
    private readonly ITextGenerationService _textGen;
    private readonly ILogger<HybridApprovalPredictor> _logger;
    private readonly IConfiguration _configuration;

    public HybridApprovalPredictor(
        ApplicationDbContext db,
        IContentFeatureRepository featureRepo,
        ITextGenerationService textGen,
        ILogger<HybridApprovalPredictor> logger,
        IConfiguration configuration)
    {
        _db = db;
        _featureRepo = featureRepo;
        _textGen = textGen;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<(double probability, string rationale)> PredictApprovalAsync(Guid clientId, Guid submissionId, CancellationToken ct = default)
    {
        // Fetch data
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == clientId, ct);
        var submission = await _db.Submissions.Include(s => s.ContentFeature).FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (client == null || submission == null)
        {
            return (0.0, "Client or submission not found");
        }

        // Gather client's past reviews
        var clientSubmissionIds = await _db.Submissions.Where(s => s.ClientId == clientId).Select(s => s.Id).ToListAsync(ct);
        var clientReviews = await _db.Reviews.Where(r => clientSubmissionIds.Contains(r.SubmissionId)).ToListAsync(ct);

        var approvedCount = clientReviews.Count(r => r.Status == ReviewStatus.Approved);
        var rejectedCount = clientReviews.Count(r => r.Status == ReviewStatus.Rejected);
        var totalClientReviews = approvedCount + rejectedCount;

        var globalApproved = await _db.Reviews.CountAsync(r => r.Status == ReviewStatus.Approved, ct);
        var globalRejected = await _db.Reviews.CountAsync(r => r.Status == ReviewStatus.Rejected, ct);
        var globalTotal = globalApproved + globalRejected;

        // Base rates
        double clientApprovalRate = totalClientReviews > 0 ? (double)approvedCount / totalClientReviews : 0.5; // neutral prior
        double globalApprovalRate = globalTotal > 0 ? (double)globalApproved / globalTotal : 0.5;

        // Tag matching boost
        var feature = submission.ContentFeature ?? await _featureRepo.GetBySubmissionIdAsync(submissionId);
        var tags = ParseTags(feature?.ThemeTagsJson);
        // Compute frequent tags from client's approved submissions
        var approvedIds = clientReviews.Where(r => r.Status == ReviewStatus.Approved).Select(r => r.SubmissionId).ToHashSet();
        var approvedFeatures = await _db.ContentFeatures.Where(f => approvedIds.Contains(f.SubmissionId)).ToListAsync(ct);
        var freq = approvedFeatures.SelectMany(f => ParseTags(f.ThemeTagsJson))
            .GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

        int matchScore = tags.Count(t => freq.ContainsKey(t));
        double tagBoost = matchScore * _configuration.GetValue<double>("Ai:Predictor:TagWeight", 0.05); // small incremental boosts

        // Simple logistic scoring
        double score = (clientApprovalRate - (1 - globalApprovalRate)) + tagBoost; // combine relative tendencies + tag match
        // clamp
        score = Math.Clamp(score, -2.0, 2.0);
        double probability = 1.0 / (1.0 + Math.Exp(-score));

        // LLM rationale (optional if model configured)
        string rationale = await BuildRationaleAsync(client, submission, tags, clientApprovalRate, probability, matchScore, ct);

        return (probability, rationale);
    }

    private async Task<string> BuildRationaleAsync(Client client, Submission submission, List<string> tags, double clientApprovalRate, double probability, int matchScore, CancellationToken ct)
    {
        var systemPrompt = "You are an assistant that explains predicted client approval likelihood for a content submission succinctly.";
        var userPrompt = $"Client Name: {client.Name ?? "(unknown)"}\nClient Approval Rate: {clientApprovalRate:P0}\nSubmission Tags: {string.Join(", ", tags)}\nTag Matches With Prior Approved Content: {matchScore}\nPredicted Probability: {probability:P0}\nProvide 2-3 bullet points rationale, referencing style or themes.";

        // If no text model configured, return static rationale
        var openAiModel = _configuration["Ai:OpenAi:Model"] ?? Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (string.IsNullOrWhiteSpace(openAiModel))
        {
            return $"Predicted approval {probability:P0}. Matches: {matchScore}. (No GPT model configured.)";
        }

        var result = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
        return string.IsNullOrWhiteSpace(result) ? $"Predicted approval {probability:P0}. Matches: {matchScore}." : result.Trim();
    }

    private List<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            return arr?.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
