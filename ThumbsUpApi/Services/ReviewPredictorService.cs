using System.Text.Json;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;

namespace ThumbsUpApi.Services;

public class ReviewPredictorService : IReviewPredictorService
{
    private readonly IClientRepository _clientRepository;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IContentFeatureRepository _featureRepo;
    private readonly IClientSummaryRepository _summaryRepo;
    private readonly ITextGenerationService _textGen;
    private readonly ILogger<ReviewPredictorService> _logger;

    public ReviewPredictorService(
        IClientRepository clientRepository,
        ISubmissionRepository submissionRepo,
        IReviewRepository reviewRepo,
        IContentFeatureRepository featureRepo,
        IClientSummaryRepository summaryRepo,
        ITextGenerationService textGen,
        ILogger<ReviewPredictorService> logger)
    {
        _clientRepository = clientRepository;
        _submissionRepo = submissionRepo;
        _reviewRepo = reviewRepo;
        _featureRepo = featureRepo;
        _summaryRepo = summaryRepo;
        _textGen = textGen;
        _logger = logger;
    }

    /// <summary>
    /// Gets or rebuilds a cached client summary with separate AI-generated insights.
    /// Rebuild conditions: no summary exists OR approved/rejected review count changed.
    /// </summary>
    public async Task<ClientSummaryResponse?> GetClientSummaryAsync(Guid clientId, string userId, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, userId);
        if (client == null)
        {
            _logger.LogWarning("Client {ClientId} not found for summary", clientId);
            return null;
        }
        
        // Load submissions and reviews through repositories
        var submissions = (await _submissionRepo.GetByClientIdAsync(clientId, userId)).ToList();
        var submissionIds = submissions.Select(s => s.Id).ToList();
        
        var reviews = (await _reviewRepo.GetByClientIdAsync(clientId, ct)).ToList();

        int approved = reviews.Count(r => r.Status == ReviewStatus.Approved);
        int rejected = reviews.Count(r => r.Status == ReviewStatus.Rejected);

        var existing = await _summaryRepo.GetByClientIdAsync(clientId);
        var countsSignature = $"{approved}:{rejected}";

        // If existing summary already encodes counts signature, parse and return cached result
        if (existing != null && existing.SummaryText.Contains($"[counts:{countsSignature}]"))
        {
            _logger.LogInformation("Using cached summary for client {ClientId}", clientId);
            var cached = DeserializeSummary(existing.SummaryText);
            if (cached != null)
            {
                cached.ClientId = clientId;
                cached.ApprovedCount = approved;
                cached.RejectedCount = rejected;
                cached.GeneratedAt = existing.UpdatedAt;
                return cached;
            }
        }
        
        // Collect data for AI generation
        var approvedIds = reviews.Where(r => r.Status == ReviewStatus.Approved)
            .Select(r => r.SubmissionId)
            .ToHashSet();
        var rejectedIds = reviews.Where(r => r.Status == ReviewStatus.Rejected)
            .Select(r => r.SubmissionId)
            .ToHashSet();

        var features = (await _featureRepo.GetBySubmissionIdsAsync(approvedIds.Union(rejectedIds).ToHashSet(), ct)).ToList();

        var approvedTags = features.Where(f => approvedIds.Contains(f.SubmissionId))
            .SelectMany(f => DeserializeTags(f.ThemeTagsJson))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => (dynamic)new { Tag = g.Key, Count = g.Count() })
            .ToList();

        var approvedComments = reviews.Where(r => r.Status == ReviewStatus.Approved && !string.IsNullOrWhiteSpace(r.Comment))
            .Select(r => r.Comment!.Trim())
            .ToList();
        var rejectedComments = reviews.Where(r => r.Status == ReviewStatus.Rejected && !string.IsNullOrWhiteSpace(r.Comment))
            .Select(r => r.Comment!.Trim())
            .ToList();

        // Generate each insight field separately with focused prompts
        var stylePreferences = await GenerateStylePreferencesAsync(client, approvedTags, approvedComments, ct);
        var recurringPositives = await GenerateRecurringPositivesAsync(client, approvedTags, approvedComments, ct);
        var rejectionReasons = await GenerateRejectionReasonsAsync(client, rejectedComments, ct);

        // Serialize and cache
        var summaryData = new
        {
            stylePreferences,
            recurringPositives,
            rejectionReasons
        };
        var summaryText = JsonSerializer.Serialize(summaryData) + $"\n[counts:{countsSignature}]";

        var newSummary = new ClientSummary
        {
            ClientId = clientId,
            SummaryText = summaryText
        };
        var persisted = await _summaryRepo.UpsertAsync(newSummary);
        await _summaryRepo.SaveChangesAsync();

        return new ClientSummaryResponse
        {
            ClientId = clientId,
            StylePreferences = stylePreferences,
            RecurringPositives = recurringPositives,
            RejectionReasons = rejectionReasons,
            ApprovedCount = approved,
            RejectedCount = rejected,
            GeneratedAt = persisted.UpdatedAt
        };
    }

    private async Task<List<string>> GenerateStylePreferencesAsync(
        Client client, 
        List<dynamic> approvedTags, 
        List<string> approvedComments, 
        CancellationToken ct)
    {
        if (approvedTags.Count == 0 && approvedComments.Count == 0)
            return new List<string> { "Insufficient data yet" };

        var systemPrompt = "You identify a client's style preferences from their approved content. Return ONLY a JSON array of 3-5 concise bullet points (strings). Each point should describe a style preference or tendency.";
        var userPrompt = $"Client: {client.Name ?? client.Email}\nApproved Tags: {string.Join(", ", approvedTags.Select(t => $"{t.Tag}({t.Count})"))}\nApproved Comments: {string.Join(" | ", approvedComments.Take(8))}\n\nIdentify their style preferences as a JSON array of strings.";
        
        try
        {
            var response = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            return ParseJsonArrayResponse(response) ?? new List<string> { "Unable to determine style preferences" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating style preferences for {ClientId}", client.Id);
            return new List<string> { "Error analyzing style preferences" };
        }
    }

    private async Task<List<string>> GenerateRecurringPositivesAsync(
        Client client, 
        List<dynamic> approvedTags, 
        List<string> approvedComments, 
        CancellationToken ct)
    {
        if (approvedTags.Count == 0 && approvedComments.Count == 0)
            return new List<string> { "Insufficient data yet" };

        var systemPrompt = "You identify recurring positive patterns in approved content. Return ONLY a JSON array of 3-5 concise bullet points (strings). Focus on what consistently works well.";
        var userPrompt = $"Client: {client.Name ?? client.Email}\nApproved Tags: {string.Join(", ", approvedTags.Select(t => $"{t.Tag}({t.Count})"))}\nApproved Comments: {string.Join(" | ", approvedComments.Take(8))}\n\nIdentify recurring positive patterns as a JSON array of strings.";
        
        try
        {
            var response = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            return ParseJsonArrayResponse(response) ?? new List<string> { "Unable to determine recurring positives" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recurring positives for {ClientId}", client.Id);
            return new List<string> { "Error analyzing positive patterns" };
        }
    }

    private async Task<List<string>> GenerateRejectionReasonsAsync(
        Client client, 
        List<string> rejectedComments, 
        CancellationToken ct)
    {
        if (rejectedComments.Count == 0)
            return new List<string> { "No rejections yet" };

        var systemPrompt = "You identify common rejection reasons from rejected content comments. Return ONLY a JSON array of 3-5 concise bullet points (strings). Focus on why content gets rejected.";
        var userPrompt = $"Client: {client.Name ?? client.Email}\nRejected Comments: {string.Join(" | ", rejectedComments.Take(10))}\n\nIdentify common rejection reasons as a JSON array of strings.";
        
        try
        {
            var response = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            return ParseJsonArrayResponse(response) ?? new List<string> { "Unable to determine rejection reasons" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating rejection reasons for {ClientId}", client.Id);
            return new List<string> { "Error analyzing rejection reasons" };
        }
    }

    private List<string>? ParseJsonArrayResponse(string response)
    {
        try
        {
            // Clean up markdown code blocks if present
            var cleaned = response.Trim();
            if (cleaned.StartsWith("```json"))
                cleaned = cleaned.Substring(7);
            if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3);
            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            cleaned = cleaned.Trim();

            var items = JsonSerializer.Deserialize<List<string>>(cleaned);
            return items?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON array response: {Response}", response.Substring(0, Math.Min(200, response.Length)));
            return null;
        }
    }

    private ClientSummaryResponse? DeserializeSummary(string summaryText)
    {
        try
        {
            // Remove counts signature
            var json = summaryText.Substring(0, summaryText.LastIndexOf("\n[counts:"));
            var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (data == null) return null;

            return new ClientSummaryResponse
            {
                StylePreferences = data.GetValueOrDefault("stylePreferences", new List<string>()),
                RecurringPositives = data.GetValueOrDefault("recurringPositives", new List<string>()),
                RejectionReasons = data.GetValueOrDefault("rejectionReasons", new List<string>())
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached summary");
            return null;
        }
    }

    private List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return list.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).ToList();
        }
        catch { return new(); }
    }
}
