using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;

namespace ThumbsUpApi.Services;

public class ReviewPredictorService
{
    private readonly ApplicationDbContext _db;
    private readonly IClientRepository _clientRepository;
    private readonly IClientSummaryRepository _summaryRepo;
    private readonly ITextGenerationService _textGen;
    private readonly ILogger<ReviewPredictorService> _logger;

    public ReviewPredictorService(
        ApplicationDbContext db,
        IClientRepository clientRepository,
        IClientSummaryRepository summaryRepo,
        ITextGenerationService textGen,
        ILogger<ReviewPredictorService> logger)
    {
        _db = db;
        _clientRepository = clientRepository;
        _summaryRepo = summaryRepo;
        _textGen = textGen;
        _logger = logger;
    }

    /// <summary>
    /// Gets or rebuilds a cached client summary. Rebuild conditions: no summary exists OR approved/rejected review count changed.
    /// </summary>
    public async Task<ClientSummary?> GetOrRefreshSummaryAsync(Guid clientId, string userId, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, userId);
        if (client == null)
        {
            _logger.LogWarning("Client {ClientId} not found for summary", clientId);
            return null;
        }
        
        // Load submissions and reviews via DbContext while enforcing
        // user-scoped client access via repository above.
        var submissions = await _db.Submissions
            .Where(s => s.ClientId == clientId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var reviews = await _db.Reviews
            .Where(r => submissions.Contains(r.SubmissionId))
            .ToListAsync(ct);

        int approved = reviews.Count(r => r.Status == ReviewStatus.Approved);
        int rejected = reviews.Count(r => r.Status == ReviewStatus.Rejected);

        var existing = await _summaryRepo.GetByClientIdAsync(clientId);
        var countsSignature = $"{approved}:{rejected}";

        // If existing summary already encodes counts signature (simple heuristic) skip regeneration.
        if (existing != null && existing.SummaryText.Contains($"[counts:{countsSignature}]"))
        {
            return existing;
        }
        // Collect tags from approved content features
        var approvedIds = reviews.Where(r => r.Status == ReviewStatus.Approved)
            .Select(r => r.SubmissionId)
            .ToHashSet();

        var features = await _db.ContentFeatures
            .Where(f => approvedIds.Contains(f.SubmissionId))
            .ToListAsync(ct);

        var tagFreq = features.SelectMany(f => DeserializeTags(f.ThemeTagsJson))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .ToList();

        var comments = reviews.Where(r => !string.IsNullOrWhiteSpace(r.Comment)).Select(r => r.Comment!.Trim()).ToList();

        var systemPrompt = "You summarize a client's stylistic and preference tendencies from structured data only.";
        var userPrompt = $"ClientName: {client.Name ?? client.Email}\nApprovedCount: {approved}\nRejectedCount: {rejected}\nTopTags: {string.Join(",", tagFreq.Select(t => t.Tag + ":" + t.Count))}\nRecentComments: {string.Join(" || ", comments.Take(10))}\nProvide: bullet points for style, recurring positives, common rejection reasons. Return under 120 words.";
        
        string summaryText;
        try
        {
            summaryText = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client summary for {ClientId}", clientId);
            summaryText = "Summary generation failed.";
        }

        // Ensure we never persist an effectively empty summary, which would
        // be stripped down to an empty string by the API layer.
        if (string.IsNullOrWhiteSpace(summaryText))
        {
            summaryText = "No meaningful review data is available yet to summarize.";
        }

        // Embed counts signature for change detection
        summaryText = summaryText.Trim() + $"\n[counts:{countsSignature}]";

        var newSummary = new ClientSummary
        {
            ClientId = clientId,
            SummaryText = summaryText
        };
        var persisted = await _summaryRepo.UpsertAsync(newSummary);
        await _summaryRepo.SaveChangesAsync();
        return persisted;
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
