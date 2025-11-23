using System.Text.Json;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

public class AiContentSummaryService : IContentSummaryService
{
    private readonly ITextGenerationService _textGeneration;
    private readonly RuleBasedContentSummaryService _fallbackService;
    private readonly ILogger<AiContentSummaryService> _logger;
    private readonly TimeSpan _timeout;

    public AiContentSummaryService(
        ITextGenerationService textGeneration,
        RuleBasedContentSummaryService fallbackService,
        ILogger<AiContentSummaryService> logger,
        IConfiguration configuration)
    {
        _textGeneration = textGeneration;
        _fallbackService = fallbackService;
        _logger = logger;
        _timeout = TimeSpan.FromMilliseconds(
            configuration.GetValue<int>("Submission:AiSummaryTimeoutMs", 2000));
    }

    public async Task<string> GenerateAsync(SubmissionResponse submission, CancellationToken cancellationToken = default)
    {
        // If analysis not complete, use fallback immediately
        var feature = submission.ContentFeature;
        if (feature == null || feature.AnalysisStatus == null || feature.AnalysisStatus == ContentFeatureStatus.Pending)
        {
            return await _fallbackService.GenerateAsync(submission, cancellationToken);
        }

        // If very small submission (1 image with minimal metadata), use fallback for speed
        if (submission.MediaFiles.Count == 1 && 
            (feature.Tags?.Count ?? 0) < 3 && 
            (feature.ThemeInsights?.Subjects?.Count ?? 0) < 2)
        {
            return await _fallbackService.GenerateAsync(submission, cancellationToken);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            var aiSummary = await GenerateAiSummaryAsync(submission, cts.Token);
            
            // Validate output is reasonable
            if (string.IsNullOrWhiteSpace(aiSummary) || aiSummary.Length < 20 || aiSummary.Length > 1000)
            {
                _logger.LogWarning("AI summary validation failed (length: {Length}), using fallback", aiSummary?.Length ?? 0);
                return await _fallbackService.GenerateAsync(submission, cancellationToken);
            }

            _logger.LogInformation("AI summary generated successfully for submission {SubmissionId}", submission.Id);
            return aiSummary;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Parent cancellation - propagate
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI summary generation failed for submission {SubmissionId}, using fallback", submission.Id);
            return await _fallbackService.GenerateAsync(submission, cancellationToken);
        }
    }

    private async Task<string> GenerateAiSummaryAsync(SubmissionResponse submission, CancellationToken cancellationToken)
    {
        var feature = submission.ContentFeature!;
        var subjects = feature.ThemeInsights?.Subjects ?? new List<string>();
        var vibes = feature.ThemeInsights?.Vibes ?? new List<string>();
        var notable = feature.ThemeInsights?.NotableElements ?? new List<string>();
        var colors = feature.ThemeInsights?.Colors ?? new List<string>();
        var keywords = feature.ThemeInsights?.Keywords ?? new List<string>();
        var tags = feature.Tags ?? new List<string>();

        // Build structured data object
        var structuredData = new
        {
            mediaCount = submission.MediaFiles.Count,
            mediaType = submission.MediaFiles.All(m => m.FileType == MediaFileType.Image) ? "image" : "media",
            subjects = subjects.Take(5).ToList(),
            vibes = vibes.Take(5).ToList(),
            notableElements = notable.Take(5).ToList(),
            colors = colors.Take(7).ToList(),
            keywords = keywords.Take(5).ToList(),
            tags = tags.Take(5).ToList(),
            userMessage = submission.Message,
            messageAlignmentTokens = CalculateAlignmentTokens(submission.Message, subjects, vibes, notable, colors, keywords, tags)
        };

        var systemPrompt = @"You are a content summarization assistant for a creative review platform. 
Your task is to generate natural, professional 2-3 sentence summaries based ONLY on the provided structured data.

Rules:
- Use ONLY the facts from the JSON data provided
- Do NOT introduce new concepts, objects, or descriptions beyond the supplied fields
- Write in a natural, flowing style (not bullet points)
- If messageAlignmentTokens is empty or has few items, include a brief suggestion to reinforce alignment
- Keep the summary between 100-300 words
- Be concise but insightful
- Focus on what's present in the content and how it relates to the user's message (if provided)";

        var userPrompt = $@"Generate a content summary for this submission:

{JsonSerializer.Serialize(structuredData, new JsonSerializerOptions { WriteIndented = true })}

Write a natural, professional summary that describes the media content and assesses alignment with the user's message.";

        var result = await _textGeneration.GenerateAsync(systemPrompt, userPrompt, cancellationToken);
        return result.Trim();
    }

    private List<string> CalculateAlignmentTokens(
        string? message,
        List<string> subjects,
        List<string> vibes,
        List<string> notable,
        List<string> colors,
        List<string> keywords,
        List<string> tags)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new List<string>();
        }

        var messageTokens = message
            .ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 2)
            .Distinct()
            .ToHashSet();

        var conceptTokens = subjects
            .Concat(vibes)
            .Concat(notable)
            .Concat(colors)
            .Concat(keywords)
            .Concat(tags)
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        return messageTokens.Intersect(conceptTokens).Take(10).ToList();
    }
}
