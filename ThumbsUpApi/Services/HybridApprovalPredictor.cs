using System.Text.Json;
using ThumbsUpApi.Configuration;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;

namespace ThumbsUpApi.Services;

public class HybridApprovalPredictor : IApprovalPredictor
{
    private readonly IContentFeatureRepository _featureRepo;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly ITextGenerationService _textGen;
    private readonly ILogger<HybridApprovalPredictor> _logger;
    private readonly AiOptions _aiOptions;
    private readonly AiPredictorOptions _predictorOptions;

    public HybridApprovalPredictor(
        IContentFeatureRepository featureRepo,
        ISubmissionRepository submissionRepo,
        IReviewRepository reviewRepo,
        ITextGenerationService textGen,
        ILogger<HybridApprovalPredictor> logger,
        Microsoft.Extensions.Options.IOptions<AiOptions> aiOptions,
        Microsoft.Extensions.Options.IOptions<AiPredictorOptions> predictorOptions)
    {
        _featureRepo = featureRepo;
        _submissionRepo = submissionRepo;
        _reviewRepo = reviewRepo;
        _textGen = textGen;
        _logger = logger;
        _aiOptions = aiOptions.Value;
        _predictorOptions = predictorOptions.Value;
    }

    public async Task<(double probability, string rationale)> PredictApprovalAsync(Guid clientId, Guid submissionId, string userId, CancellationToken ct = default)
    {
        // Fetch data with user permission check
        var submission = await _submissionRepo.GetByIdWithIncludesAsync(submissionId, userId);
        var client = submission?.Client;
        if (client == null || submission == null)
        {
            return (0.0, "Client or submission not found");
        }

        // Gather client's past reviews and global stats via repository
        (int approvedCount, int rejectedCount, int totalClientReviews) = await _reviewRepo.GetClientStatsAsync(clientId, ct);
        (int globalApproved, int globalRejected, int globalTotal) = await _reviewRepo.GetGlobalStatsAsync(ct);

        // Base rates
        double clientApprovalRate = totalClientReviews > 0 ? (double)approvedCount / totalClientReviews : 0.5; // neutral prior
        double globalApprovalRate = globalTotal > 0 ? (double)globalApproved / globalTotal : 0.5;

        // Tag matching boost
        var feature = submission.ContentFeature ?? await _featureRepo.GetBySubmissionIdAsync(submissionId);
        var tags = ParseTags(feature?.ThemeTagsJson);
        // Compute frequent tags from client's approved submissions
        var clientSubmissionIds = await _submissionRepo.GetByClientIdAsync(clientId, userId);
        var approvedIds = clientSubmissionIds
            .SelectMany(s => s.Review != null && s.Review.Status == ReviewStatus.Approved ? new[] { s.Id } : Array.Empty<Guid>())
            .ToHashSet();
        var approvedFeatures = new List<ContentFeature>();
        foreach (var id in approvedIds)
        {
            var f = await _featureRepo.GetBySubmissionIdAsync(id);
            if (f != null) approvedFeatures.Add(f);
        }
        var freq = approvedFeatures.SelectMany(f => ParseTags(f.ThemeTagsJson))
            .GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

        int matchScore = tags.Count(t => freq.ContainsKey(t));
        double tagBoost = matchScore * _predictorOptions.TagWeight;

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
        var openAiModel = _aiOptions.TextModel ?? Environment.GetEnvironmentVariable("OPENAI_MODEL");
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
