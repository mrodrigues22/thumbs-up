using System;
using System.Text.Json;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;
using Microsoft.Extensions.Hosting;

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
    private readonly IHostEnvironment _hostEnvironment;

    public ReviewPredictorService(
        IClientRepository clientRepository,
        ISubmissionRepository submissionRepo,
        IReviewRepository reviewRepo,
        IContentFeatureRepository featureRepo,
        IClientSummaryRepository summaryRepo,
        ITextGenerationService textGen,
        ILogger<ReviewPredictorService> logger,
        IHostEnvironment hostEnvironment)
    {
        _clientRepository = clientRepository;
        _submissionRepo = submissionRepo;
        _reviewRepo = reviewRepo;
        _featureRepo = featureRepo;
        _summaryRepo = summaryRepo;
        _textGen = textGen;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
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
        var skipCache = _hostEnvironment.IsDevelopment();

        if (!skipCache && existing != null && existing.SummaryText.Contains($"[counts:{countsSignature}]"))
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

        var reviewedIds = approvedIds.Union(rejectedIds).ToHashSet();
        var coverage = AssessSummaryCoverage(approved + rejected, reviewedIds, features);
        var missingSignals = new List<string>(coverage.MissingSignals);

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

        if (approved > 0 && approvedTags.Count == 0 && approvedComments.Count == 0)
        {
            AddMissingSignal(missingSignals, "Need more analyzed approved submissions to describe style preferences.");
        }

        if (rejected > 0 && rejectedComments.Count == 0)
        {
            AddMissingSignal(missingSignals, "Rejected submissions do not include reviewer comments yet.");
        }

        var skipAiInsights = coverage.Status is SummaryDataStatus.PendingAnalysis or SummaryDataStatus.InsufficientHistory;

        var stylePreferences = new List<string>();
        var recurringPositives = new List<string>();
        List<string> rejectionReasons;

        if (!skipAiInsights && (approvedTags.Count > 0 || approvedComments.Count > 0))
        {
            stylePreferences = await GenerateStylePreferencesAsync(client, approvedTags, approvedComments, ct);
            recurringPositives = await GenerateRecurringPositivesAsync(client, approvedTags, approvedComments, ct);
        }

        if (!skipAiInsights && rejectedComments.Count > 0)
        {
            rejectionReasons = await GenerateRejectionReasonsAsync(client, rejectedComments, ct);
        }
        else if (rejected == 0)
        {
            rejectionReasons = new List<string> { "No rejections yet" };
        }
        else
        {
            rejectionReasons = new List<string>();
        }

        if (stylePreferences.Count == 0 && recurringPositives.Count == 0 && !skipAiInsights && approvedTags.Count == 0 && approvedComments.Count == 0)
        {
            AddMissingSignal(missingSignals, "Need tags or reviewer notes on approved submissions.");
        }

        // Serialize and cache
        var summaryData = new
        {
            stylePreferences,
            recurringPositives,
            rejectionReasons,
            dataStatus = coverage.Status,
            missingSignals,
            pendingAnalysisCount = coverage.PendingAnalysisCount,
            featureCoverageCount = coverage.FeatureCoverageCount
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
            GeneratedAt = persisted.UpdatedAt,
            DataStatus = coverage.Status,
            MissingSignals = missingSignals,
            PendingAnalysisCount = coverage.PendingAnalysisCount,
            FeatureCoverageCount = coverage.FeatureCoverageCount
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

        var systemPrompt = "You are an experienced art director for a social media company. Identify a client's style preferences from their approved content. Return ONLY a JSON array of 3-5 concise bullet points (strings). Each point should describe a style preference or tendency.";
        var userPrompt = $"Client: {client.Name ?? client.Email}\nApproved Tags: {string.Join(", ", approvedTags.Select(t => $"{t.Tag}({t.Count})"))}\nApproved Comments: {string.Join(" | ", approvedComments.Take(8))}\n\nIdentify their style preferences as a JSON array of strings.";
        
        try
        {
            var response = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("Empty response from AI for style preferences. Client: {ClientId}", client.Id);
                return new List<string>();
            }
            
            var parsed = ParseJsonArrayResponse(response);
            if (parsed == null)
            {
                _logger.LogWarning("Failed to parse style preferences response. Response: {Response}", response.Substring(0, Math.Min(500, response.Length)));
                return new List<string>();
            }
            return parsed;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            // Propagate configuration errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating style preferences for {ClientId}", client.Id);
            return new List<string>();
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

        var systemPrompt = "You are an experienced art director for a social media company. You identify recurring positive patterns in approved content. Return ONLY a JSON array of 3-5 concise bullet points (strings). Focus on what consistently works well.";
        var userPrompt = $"Client: {client.Name ?? client.Email}\nApproved Tags: {string.Join(", ", approvedTags.Select(t => $"{t.Tag}({t.Count})"))}\nApproved Comments: {string.Join(" | ", approvedComments.Take(8))}\n\nIdentify recurring positive patterns as a JSON array of strings.";
        
        try
        {
            var response = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("Empty response from AI for recurring positives. Client: {ClientId}", client.Id);
                return new List<string>();
            }
            
            var parsed = ParseJsonArrayResponse(response);
            if (parsed == null)
            {
                _logger.LogWarning("Failed to parse recurring positives response. Response: {Response}", response.Substring(0, Math.Min(500, response.Length)));
                return new List<string>();
            }
            return parsed;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            // Propagate configuration errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recurring positives for {ClientId}", client.Id);
            return new List<string>();
        }
    }

    private async Task<List<string>> GenerateRejectionReasonsAsync(
        Client client, 
        List<string> rejectedComments, 
        CancellationToken ct)
    {
        if (rejectedComments.Count == 0)
            return new List<string> { "No rejections yet" };

        var systemPrompt = "You are an experienced art director for a social media company.You identify common rejection reasons from rejected content comments. Return ONLY a JSON array of 3-5 concise bullet points (strings). Focus on why content gets rejected.";
        var userPrompt = $"Client: {client.Name ?? client.Email}\nRejected Comments: {string.Join(" | ", rejectedComments.Take(10))}\n\nIdentify common rejection reasons as a JSON array of strings.";
        
        try
        {
            var response = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("Empty response from AI for rejection reasons. Client: {ClientId}", client.Id);
                return new List<string>();
            }
            
            var parsed = ParseJsonArrayResponse(response);
            if (parsed == null)
            {
                _logger.LogWarning("Failed to parse rejection reasons response. Response: {Response}", response.Substring(0, Math.Min(500, response.Length)));
                return new List<string>();
            }
            return parsed;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            // Propagate configuration errors
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating rejection reasons for {ClientId}", client.Id);
            return new List<string>();
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
            var markerIndex = summaryText.LastIndexOf("\n[counts:", StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var json = summaryText[..markerIndex];
            var cached = JsonSerializer.Deserialize<ClientSummaryCache>(json);
            if (cached != null)
            {
                return new ClientSummaryResponse
                {
                    StylePreferences = cached.StylePreferences ?? new List<string>(),
                    RecurringPositives = cached.RecurringPositives ?? new List<string>(),
                    RejectionReasons = cached.RejectionReasons ?? new List<string>(),
                    DataStatus = cached.DataStatus,
                    MissingSignals = cached.MissingSignals ?? new List<string>(),
                    PendingAnalysisCount = cached.PendingAnalysisCount,
                    FeatureCoverageCount = cached.FeatureCoverageCount
                };
            }

            var legacy = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (legacy == null)
            {
                return null;
            }

            return new ClientSummaryResponse
            {
                StylePreferences = legacy.GetValueOrDefault("stylePreferences", new List<string>()),
                RecurringPositives = legacy.GetValueOrDefault("recurringPositives", new List<string>()),
                RejectionReasons = legacy.GetValueOrDefault("rejectionReasons", new List<string>()),
                DataStatus = SummaryDataStatus.PendingAnalysis,
                MissingSignals = new List<string>()
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
        return ThemeInsights.FromJson(json)
            .FlattenTags()
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .ToList();
    }

    private SummaryCoverage AssessSummaryCoverage(int totalReviewed, HashSet<Guid> reviewedIds, List<ContentFeature> features)
    {
        var featureLookup = features.ToDictionary(f => f.SubmissionId, f => f);
        var missingFeatures = reviewedIds.Count(id => !featureLookup.ContainsKey(id));

        var coverageCount = features.Count(f => f.AnalysisStatus == ContentFeatureStatus.Completed || f.AnalysisStatus == ContentFeatureStatus.NoSignals);
        var pendingCount = features.Count(f => f.AnalysisStatus == ContentFeatureStatus.Pending) + missingFeatures;
        var failedCount = features.Count(f => f.AnalysisStatus == ContentFeatureStatus.Failed);
        var noImageCount = features.Count(f => f.AnalysisStatus == ContentFeatureStatus.NoImages);

        var signals = new List<string>();
        SummaryDataStatus status;

        if (totalReviewed == 0)
        {
            status = SummaryDataStatus.InsufficientHistory;
            signals.Add("Need at least one approved or rejected submission.");
        }
        else if (coverageCount == 0 && pendingCount > 0)
        {
            status = SummaryDataStatus.PendingAnalysis;
            signals.Add($"Image analysis pending for {pendingCount} submission(s).");
        }
        else if (coverageCount == 0)
        {
            status = SummaryDataStatus.Partial;
            if (failedCount > 0)
            {
                signals.Add($"Image analysis failed on {failedCount} submission(s).");
            }
        }
        else if (pendingCount > 0 || failedCount > 0)
        {
            status = SummaryDataStatus.Partial;
            if (pendingCount > 0)
            {
                signals.Add($"Image analysis pending for {pendingCount} submission(s).");
            }
            if (failedCount > 0)
            {
                signals.Add($"Image analysis failed on {failedCount} submission(s).");
            }
        }
        else
        {
            status = SummaryDataStatus.Ready;
        }

        if (noImageCount > 0)
        {
            signals.Add($"{noImageCount} submission(s) did not include analyzable image files.");
        }

        return new SummaryCoverage
        {
            Status = status,
            MissingSignals = signals.Distinct().ToList(),
            PendingAnalysisCount = pendingCount,
            FeatureCoverageCount = coverageCount
        };
    }

    private static void AddMissingSignal(List<string> signals, string message)
    {
        if (!signals.Contains(message))
        {
            signals.Add(message);
        }
    }

    private sealed class SummaryCoverage
    {
        public SummaryDataStatus Status { get; init; }
        public List<string> MissingSignals { get; init; } = new();
        public int PendingAnalysisCount { get; init; }
        public int FeatureCoverageCount { get; init; }
    }

    private sealed class ClientSummaryCache
    {
        public List<string>? StylePreferences { get; set; }
        public List<string>? RecurringPositives { get; set; }
        public List<string>? RejectionReasons { get; set; }
        public SummaryDataStatus DataStatus { get; set; } = SummaryDataStatus.PendingAnalysis;
        public List<string>? MissingSignals { get; set; }
        public int PendingAnalysisCount { get; set; }
        public int FeatureCoverageCount { get; set; }
    }
}
