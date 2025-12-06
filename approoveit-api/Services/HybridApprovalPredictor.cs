using System.Globalization;
using System.Text;
using ApprooveItApi.Configuration;
using ApprooveItApi.DTOs;
using ApprooveItApi.Interfaces;
using ApprooveItApi.Models;
using ApprooveItApi.Repositories;

namespace ApprooveItApi.Services;

public class HybridApprovalPredictor : IApprovalPredictor
{
    private static readonly char[] TokenDelimiters = new[] { ' ', ',', '.', ';', ':', '!', '?', '/', '\\', '-', '_', '|', '\n', '\r', '\t' };
    private static readonly char[] TrimChars = new[] { '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}' };
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "the", "with", "for", "this", "that", "from", "into", "your", "their", "about", "over",
        "some", "more", "than", "very", "much", "also", "just", "when", "after", "before", "were", "been",
        "them", "they", "you", "our", "any", "but", "are", "was", "has", "have", "had", "will", "would",
        "could", "should", "onto", "while", "there", "here"
    };
    private static readonly TextInfo TitleCase = CultureInfo.InvariantCulture.TextInfo;

    private readonly IContentFeatureRepository _featureRepo;
    private readonly ISubmissionRepository _submissionRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IReviewPredictorService _reviewPredictor;
    private readonly ITextGenerationService _textGen;
    private readonly ILogger<HybridApprovalPredictor> _logger;
    private readonly AiOptions _aiOptions;
    private readonly AiPredictorOptions _predictorOptions;

    public HybridApprovalPredictor(
        IContentFeatureRepository featureRepo,
        ISubmissionRepository submissionRepo,
        IReviewRepository reviewRepo,
        IReviewPredictorService reviewPredictor,
        ITextGenerationService textGen,
        ILogger<HybridApprovalPredictor> logger,
        Microsoft.Extensions.Options.IOptions<AiOptions> aiOptions,
        Microsoft.Extensions.Options.IOptions<AiPredictorOptions> predictorOptions)
    {
        _featureRepo = featureRepo;
        _submissionRepo = submissionRepo;
        _reviewRepo = reviewRepo;
        _reviewPredictor = reviewPredictor;
        _textGen = textGen;
        _logger = logger;
        _aiOptions = aiOptions.Value;
        _predictorOptions = predictorOptions.Value;
    }

    public async Task<ApprovalPredictionResponse> PredictApprovalAsync(Guid clientId, Guid submissionId, string userId, CancellationToken ct = default)
    {
        // Fetch data with user permission check
        var submission = await _submissionRepo.GetByIdWithIncludesAsync(submissionId, userId);
        var client = submission?.Client;
        if (client == null || submission == null)
        {
            return new ApprovalPredictionResponse
            {
                ClientId = clientId,
                SubmissionId = submissionId,
                Probability = null,
                Rationale = "- Client or submission not found.",
                Status = ApprovalPredictionStatus.Error,
                StatusMessage = "Client or submission not found."
            };
        }

        // Gather client's past reviews and global stats via repository
        (int approvedCount, int rejectedCount, int totalClientReviews) = await _reviewRepo.GetClientStatsAsync(clientId, ct);
        (int globalApproved, int globalRejected, int globalTotal) = await _reviewRepo.GetGlobalStatsAsync(ct);

        // Base rates
        double clientApprovalRate = totalClientReviews > 0 ? (double)approvedCount / totalClientReviews : 0.5; // neutral prior
        double globalApprovalRate = globalTotal > 0 ? (double)globalApproved / globalTotal : 0.5;

        // Theme matching boost
        var feature = submission.ContentFeature ?? await _featureRepo.GetBySubmissionIdAsync(submissionId);
        var readiness = EvaluateReadiness(feature);
        if (!readiness.IsReady)
        {
            return new ApprovalPredictionResponse
            {
                ClientId = clientId,
                SubmissionId = submissionId,
                Probability = null,
                Rationale = $"- {readiness.Message}",
                Status = readiness.Status,
                StatusMessage = readiness.Message
            };
        }

        var tags = ParseTags(feature?.ThemeTagsJson);
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
        var friendlyThemes = tags
            .Select(ToFriendlyTheme)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var submissionTerms = BuildSubmissionTerms(tags, submission, feature?.OcrText);
        var summary = await _reviewPredictor.GetClientSummaryAsync(clientId, userId, ct);
        var summaryInfluence = summary != null && submissionTerms.Count > 0
            ? CalculateSummaryInfluence(summary, submissionTerms)
            : new SummaryInfluenceResult();

        // Simple logistic scoring with summary boost
        double score = (clientApprovalRate - (1 - globalApprovalRate)) + tagBoost + summaryInfluence.Boost;
        score = Math.Clamp(score, -2.0, 2.0);
        double probability = 1.0 / (1.0 + Math.Exp(-score));

        // LLM rationale (optional if model configured)
        string rationale = await BuildRationaleAsync(
            client,
            submission,
            friendlyThemes,
            clientApprovalRate,
            probability,
            matchScore,
            summary,
            summaryInfluence,
            ct);

        return new ApprovalPredictionResponse
        {
            ClientId = clientId,
            SubmissionId = submissionId,
            Probability = probability,
            Rationale = rationale,
            Status = ApprovalPredictionStatus.Ready,
            StatusMessage = readiness.LimitedSignals
                ? "Scored with limited signals."
                : "Scored using latest client and submission signals."
        };
    }

    private async Task<string> BuildRationaleAsync(
        Client client,
        Submission submission,
        List<string> friendlyThemes,
        double clientApprovalRate,
        double probability,
        int alignedThemeCount,
        ClientSummaryResponse? summary,
        SummaryInfluenceResult summaryInfluence,
        CancellationToken ct)
    {
        var systemPrompt = "You are an experienced art director for a social media company.You advise account managers on how likely a client is to approve new creative. " +
            "Respond with 2-3 short bullet points (max 20 words each). Keep the tone plain, avoid jargon like tags or metadata, " +
            "and reference the client's documented preferences when it helps.";

        var submissionNarrative = Truncate(BuildSubmissionNarrative(submission), 320);
        var userPrompt = $"""
Client Name: {client.Name ?? "(unknown)"}
Historical Approval Rate: {clientApprovalRate:P0}
Submission Themes: {CollapseList(friendlyThemes, "No extracted themes")}
Submission Notes: {submissionNarrative}
Documented Style Preferences: {CollapseList(summary?.StylePreferences, "No documented style preferences")}
Recurring Positives: {CollapseList(summary?.RecurringPositives, "No recurring positives yet")}
Rejection Reasons: {CollapseList(summary?.RejectionReasons, "No rejection reasons recorded")}
Matched Client Themes Count: {alignedThemeCount}
Aligned Summary Signals: {CollapseList(summaryInfluence.PositiveSignals, "None detected for this submission")}
Potential Risks: {CollapseList(summaryInfluence.RiskSignals, "No specific risks detected")}
Predicted Probability: {probability:P0}

Write the bullets now, starting each line with "- " and never mentioning metadata or scoring formulas.
""";

        var openAiModel = _aiOptions.TextModel ?? Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (string.IsNullOrWhiteSpace(openAiModel))
        {
            return BuildFallbackRationale(probability, clientApprovalRate, friendlyThemes, summary, summaryInfluence, alignedThemeCount);
        }

        try
        {
            var result = await _textGen.GenerateAsync(systemPrompt, userPrompt, ct);
            if (string.IsNullOrWhiteSpace(result))
            {
                return BuildFallbackRationale(probability, clientApprovalRate, friendlyThemes, summary, summaryInfluence, alignedThemeCount);
            }
            return result.Trim();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI rationale for submission {SubmissionId}", submission.Id);
            return BuildFallbackRationale(probability, clientApprovalRate, friendlyThemes, summary, summaryInfluence, alignedThemeCount);
        }
    }

    private string BuildFallbackRationale(
        double probability,
        double clientApprovalRate,
        List<string> friendlyThemes,
        ClientSummaryResponse? summary,
        SummaryInfluenceResult influence,
        int alignedThemeCount)
    {
        var bullets = new List<string>
        {
            $"Approval likelihood is {probability:P0}, in line with their {clientApprovalRate:P0} history."
        };

        if (alignedThemeCount > 0)
        {
            bullets.Add($"It mirrors {alignedThemeCount} themes they have approved recently.");
        }
        else if (friendlyThemes.Count > 0)
        {
            bullets.Add($"Themes in play: {string.Join(", ", friendlyThemes.Take(3))}.");
        }

        if (influence.PositiveSignals.Count > 0)
        {
            bullets.Add($"Helps: {string.Join("; ", influence.PositiveSignals.Take(2))}.");
        }
        else if (summary?.StylePreferences?.Any() == true)
        {
            bullets.Add($"They respond well to {string.Join("; ", summary.StylePreferences.Take(2))}.");
        }

        if (influence.RiskSignals.Count > 0)
        {
            bullets.Add($"Watch-outs: {string.Join("; ", influence.RiskSignals.Take(2))}.");
        }
        else if (summary?.RejectionReasons?.Any() == true)
        {
            bullets.Add($"Avoid {summary.RejectionReasons.First()}.");
        }

        return string.Join("\n", bullets.Select(b => $"- {b}"));
    }

    private PredictionReadiness EvaluateReadiness(ContentFeature? feature)
    {
        if (feature == null)
        {
            return new PredictionReadiness(false, ApprovalPredictionStatus.PendingSignals, "Waiting for image analysis to complete.", false);
        }

        return feature.AnalysisStatus switch
        {
            ContentFeatureStatus.Completed => new PredictionReadiness(true, ApprovalPredictionStatus.Ready, "Ready.", false),
            ContentFeatureStatus.NoSignals => new PredictionReadiness(true, ApprovalPredictionStatus.Ready, "Limited signals detected for this submission.", true),
            ContentFeatureStatus.Pending => new PredictionReadiness(false, ApprovalPredictionStatus.PendingSignals, "Image analysis still running for this submission.", false),
            ContentFeatureStatus.Failed => new PredictionReadiness(false, ApprovalPredictionStatus.PendingSignals, feature.FailureReason ?? "Image analysis failed.", false),
            ContentFeatureStatus.NoImages => new PredictionReadiness(false, ApprovalPredictionStatus.MissingHistory, "Submission has no image files to analyze.", false),
            _ => new PredictionReadiness(false, ApprovalPredictionStatus.PendingSignals, "Waiting for analyzable signals.", false)
        };
    }

    private List<string> ParseTags(string? json)
    {
        return ThemeInsights.FromJson(json)
            .FlattenTags()
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();
    }

    private SummaryInfluenceResult CalculateSummaryInfluence(ClientSummaryResponse summary, HashSet<string> submissionTerms)
    {
        if (submissionTerms.Count == 0)
        {
            return new SummaryInfluenceResult();
        }

        var positiveMatches = CaptureMatches(Enumerate(summary.StylePreferences).Concat(Enumerate(summary.RecurringPositives)), submissionTerms);
        var riskMatches = CaptureMatches(Enumerate(summary.RejectionReasons), submissionTerms);

        double boost = positiveMatches.Count * _predictorOptions.SummaryAlignmentWeight;
        double penalty = riskMatches.Count * _predictorOptions.SummaryPenaltyWeight;

        return new SummaryInfluenceResult
        {
            Boost = boost - penalty,
            PositiveSignals = positiveMatches.Take(3).ToList(),
            RiskSignals = riskMatches.Take(3).ToList()
        };
    }

    private static List<string> CaptureMatches(IEnumerable<string>? phrases, HashSet<string> submissionTerms)
    {
        if (phrases == null)
        {
            return new List<string>();
        }

        var matches = new List<string>();
        foreach (var phrase in phrases)
        {
            if (string.IsNullOrWhiteSpace(phrase)) continue;
            if (PhraseMatchesSubmission(phrase, submissionTerms))
            {
                matches.Add(phrase.Trim());
            }
        }
        return matches;
    }

    private static bool PhraseMatchesSubmission(string phrase, HashSet<string> submissionTerms)
    {
        foreach (var token in Tokenize(phrase))
        {
            if (submissionTerms.Contains(token))
            {
                return true;
            }
        }
        return false;
    }

    private static HashSet<string> BuildSubmissionTerms(IEnumerable<string> tags, Submission submission, string? ocrText)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            foreach (var token in Tokenize(tag))
            {
                terms.Add(token);
            }
        }

        if (!string.IsNullOrWhiteSpace(submission.Message))
        {
            foreach (var token in Tokenize(submission.Message))
            {
                terms.Add(token);
            }
        }

        if (!string.IsNullOrWhiteSpace(submission.Captions))
        {
            foreach (var token in Tokenize(submission.Captions))
            {
                terms.Add(token);
            }
        }

        if (!string.IsNullOrWhiteSpace(ocrText))
        {
            foreach (var token in Tokenize(ocrText))
            {
                terms.Add(token);
            }
        }

        return terms;
    }

    private static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var tokens = text.ToLowerInvariant().Split(TokenDelimiters, StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            var cleaned = token.Trim(TrimChars);
            if (cleaned.Length <= 2) continue;
            if (StopWords.Contains(cleaned)) continue;
            yield return cleaned;
        }
    }

    private static string BuildSubmissionNarrative(Submission submission)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(submission.Message))
        {
            builder.Append(submission.Message.Trim());
        }

        if (!string.IsNullOrWhiteSpace(submission.Captions))
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }
            builder.Append(submission.Captions.Trim());
        }

        return builder.Length == 0 ? "No additional notes provided." : builder.ToString();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private static string CollapseList(IEnumerable<string>? items, string fallback)
    {
        if (items == null)
        {
            return fallback;
        }

        var list = items.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList();
        return list.Count == 0 ? fallback : string.Join(" | ", list);
    }

    private static IEnumerable<string> Enumerate(IEnumerable<string>? source) => source ?? Array.Empty<string>();

    private static string ToFriendlyTheme(string rawTheme)
    {
        if (string.IsNullOrWhiteSpace(rawTheme))
        {
            return string.Empty;
        }

        var cleaned = rawTheme.Replace('_', ' ').Replace('-', ' ').Trim();
        if (cleaned.Length == 0)
        {
            return string.Empty;
        }

        return TitleCase.ToTitleCase(cleaned);
    }

    private sealed class SummaryInfluenceResult
    {
        public double Boost { get; init; }
        public List<string> PositiveSignals { get; init; } = new();
        public List<string> RiskSignals { get; init; } = new();
    }

    private sealed record PredictionReadiness(bool IsReady, ApprovalPredictionStatus Status, string Message, bool LimitedSignals);
}
