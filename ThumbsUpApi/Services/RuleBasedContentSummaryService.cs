using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

public class RuleBasedContentSummaryService : IContentSummaryService
{
    private readonly ILogger<RuleBasedContentSummaryService> _logger;

    public RuleBasedContentSummaryService(ILogger<RuleBasedContentSummaryService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateAsync(SubmissionResponse submission, CancellationToken cancellationToken = default)
    {
        var summary = BuildContentSummary(submission);
        return Task.FromResult(summary);
    }

    private string BuildContentSummary(SubmissionResponse submission)
    {
        if (submission.MediaFiles == null || submission.MediaFiles.Count == 0)
        {
            return "No media files were attached.";
        }

        var feature = submission.ContentFeature;
        if (feature == null || feature.AnalysisStatus == null || feature.AnalysisStatus == ContentFeatureStatus.Pending)
        {
            return "Content analysis pending. Media uploaded and awaiting processing.";
        }

        var subjects = feature.ThemeInsights?.Subjects ?? new List<string>();
        var vibes = feature.ThemeInsights?.Vibes ?? new List<string>();
        var notable = feature.ThemeInsights?.NotableElements ?? new List<string>();
        var colors = feature.ThemeInsights?.Colors ?? new List<string>();
        var keywords = feature.ThemeInsights?.Keywords ?? new List<string>();
        var tags = feature.Tags ?? new List<string>();

        var mediaKind = submission.MediaFiles.All(m => m.FileType == MediaFileType.Image) ? "image" : "media";
        var count = submission.MediaFiles.Count;

        string baseDesc = $"This {mediaKind} submission contains {count} {(count == 1 ? mediaKind : mediaKind + "s")}";

        if (subjects.Count > 0)
        {
            baseDesc += $" featuring {string.Join(", ", subjects.Take(5))}";
        }

        if (notable.Count > 0)
        {
            baseDesc += $". Notable elements include {string.Join(", ", notable.Take(5))}";
        }

        if (colors.Count > 0)
        {
            baseDesc += $". Dominant colors: {string.Join(", ", colors.Take(7))}";
        }

        if (vibes.Count > 0)
        {
            baseDesc += $". Overall vibe: {string.Join(", ", vibes.Take(5))}";
        }

        // Alignment analysis with client expectations (using message if provided)
        var message = submission.Message;
        string alignment;
        if (!string.IsNullOrWhiteSpace(message))
        {
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

            var overlap = messageTokens.Intersect(conceptTokens).Take(10).ToList();
            if (overlap.Count > 0)
            {
                alignment = $"The visuals align with the stated message through concepts such as: {string.Join(", ", overlap)}.";
            }
            else
            {
                alignment = "The current visual themes only partially reflect the stated message; consider reinforcing key terms or adding supporting elements.";
            }
        }
        else
        {
            alignment = "No message provided to assess alignment.";
        }

        return baseDesc + ". " + alignment;
    }
}
