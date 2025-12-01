using System.Text.Json;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;
using ThumbsUpApi.Repositories;

namespace ThumbsUpApi.Services;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly ISubmissionRepository _submissionRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly IImageOcrService _ocr;
    private readonly IImageThemeService _themes;
    private readonly IContentFeatureRepository _featureRepo;
    private readonly ILogger<ImageAnalysisService> _logger;

    public ImageAnalysisService(
        ISubmissionRepository submissionRepo,
        IFileStorageService fileStorage,
        IImageOcrService ocr,
        IImageThemeService themes,
        IContentFeatureRepository featureRepo,
        ILogger<ImageAnalysisService> logger)
    {
        _submissionRepo = submissionRepo;
        _fileStorage = fileStorage;
        _ocr = ocr;
        _themes = themes;
        _featureRepo = featureRepo;
        _logger = logger;
    }

    /// <summary>
    /// Runs OCR + theme extraction for image media files of a submission and persists aggregate feature.
    /// Safe to call multiple times; it upserts the feature row.
    /// </summary>
    public async Task<ContentFeature?> AnalyzeSubmissionAsync(Guid submissionId, string userId, CancellationToken ct = default)
    {
        var submission = await _submissionRepo.GetByIdWithIncludesAsync(submissionId, userId);
        if (submission == null)
        {
            _logger.LogWarning("Submission {SubmissionId} not found for analysis", submissionId);
            return null;
        }

        return await AnalyzeAndPersistAsync(submission, ct);
    }

    /// <summary>
    /// Analyzes a submission without userId restriction (for internal/background processing).
    /// </summary>
    public async Task<ContentFeature?> AnalyzeSubmissionAsync(Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _submissionRepo.GetByIdInternalAsync(submissionId);
        if (submission == null)
        {
            _logger.LogWarning("Submission {SubmissionId} not found for analysis", submissionId);
            return null;
        }

        return await AnalyzeAndPersistAsync(submission, ct);
    }

    private async Task<ContentFeature?> AnalyzeAndPersistAsync(Submission submission, CancellationToken ct)
    {
        var imageFiles = submission.MediaFiles.Where(m => m.FileType == MediaFileType.Image).ToList();
        var attemptTimestamp = DateTime.UtcNow;
        if (imageFiles.Count == 0)
        {
            _logger.LogInformation("Submission {SubmissionId} has no image files for analysis", submission.Id);
            var emptyFeature = new ContentFeature
            {
                SubmissionId = submission.Id,
                OcrText = null,
                ThemeTagsJson = null,
                AnalysisStatus = ContentFeatureStatus.NoImages,
                FailureReason = "Submission has no image files.",
                LastAnalyzedAt = attemptTimestamp,
                ExtractedAt = null
            };
            var storedEmpty = await _featureRepo.UpsertAsync(emptyFeature);
            await _featureRepo.SaveChangesAsync();
            return storedEmpty;
        }

        var analysisTasks = imageFiles
            .Select(media => AnalyzeMediaFileAsync(submission.Id, media, ct))
            .ToList();

        var results = await Task.WhenAll(analysisTasks);

        var ocrTexts = results
            .Select(r => r.OcrText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!.Trim())
            .ToList();

        var combinedInsights = ThemeInsights.Combine(results.Select(r => r.Insights));
        var flattenedTags = combinedInsights
            .FlattenTags()
            .Select(tag => tag.ToLowerInvariant())
            .Where(tag => tag.Length > 0 && tag.Length <= 50)
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();

        var hasSignals = ocrTexts.Count > 0 || combinedInsights.HasAnyData;
        var feature = new ContentFeature
        {
            SubmissionId = submission.Id,
            OcrText = ocrTexts.Count == 0 ? null : string.Join("\n", ocrTexts),
            ThemeTagsJson = BuildThemePayload(combinedInsights, flattenedTags),
            LastAnalyzedAt = attemptTimestamp
        };

        if (hasSignals)
        {
            feature.AnalysisStatus = ContentFeatureStatus.Completed;
            feature.FailureReason = null;
            feature.ExtractedAt = attemptTimestamp;
        }
        else
        {
            var hadAnySuccess = results.Any(r => r.OcrSucceeded || r.ThemeSucceeded);
            var aggregatedErrors = AggregateErrors(results);

            if (hadAnySuccess)
            {
                feature.AnalysisStatus = ContentFeatureStatus.NoSignals;
                feature.FailureReason = "Analyzed images but no text or visual signals detected.";
                feature.ExtractedAt = attemptTimestamp;
            }
            else
            {
                feature.AnalysisStatus = ContentFeatureStatus.Failed;
                feature.FailureReason = aggregatedErrors ?? "Analysis failed for all images.";
                feature.ExtractedAt = null;
            }
        }

        var persisted = await _featureRepo.UpsertAsync(feature);
        await _featureRepo.SaveChangesAsync();

        _logger.LogInformation(
            "ContentFeature stored for submission {SubmissionId} with {TagCount} tags and {OcrCount} OCR segments",
            submission.Id,
            flattenedTags.Count,
            ocrTexts.Count);

        return persisted;
    }

    private async Task<MediaAnalysisResult> AnalyzeMediaFileAsync(Guid submissionId, MediaFile media, CancellationToken ct)
    {
        var physical = _fileStorage.GetPhysicalPath(media.FilePath);

        string? ocrText = null;
        ThemeInsights insights = ThemeInsights.Empty;
        bool ocrSucceeded = false;
        bool themeSucceeded = false;
        var errors = new List<string>();

        try
        {
            ocrText = await _ocr.ExtractTextAsync(physical, ct);
            ocrSucceeded = !string.IsNullOrWhiteSpace(ocrText);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR extraction failed for media {MediaId} (submission {SubmissionId})", media.Id, submissionId);
            errors.Add($"ocr:{ex.GetType().Name}");
        }

        try
        {
            insights = await _themes.ExtractThemesAsync(physical, ct);
            themeSucceeded = insights.HasAnyData;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Theme extraction failed for media {MediaId} (submission {SubmissionId})", media.Id, submissionId);
            errors.Add($"themes:{ex.GetType().Name}");
            insights = ThemeInsights.Empty;
        }

        return new MediaAnalysisResult(ocrText, insights, ocrSucceeded, themeSucceeded, errors);
    }

    private static string? AggregateErrors(IEnumerable<MediaAnalysisResult> results)
    {
        var distinct = results
            .SelectMany(r => r.Errors)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        if (distinct.Count == 0)
        {
            return null;
        }

        return "Image analysis failed: " + string.Join(", ", distinct.Take(4));
    }

    private static string? BuildThemePayload(ThemeInsights insights, IReadOnlyCollection<string> fallbackTags)
    {
        if (insights.HasAnyData)
        {
            return insights.ToJson();
        }

        return fallbackTags.Count == 0 ? null : JsonSerializer.Serialize(fallbackTags);
    }

    private sealed record MediaAnalysisResult(string? OcrText, ThemeInsights Insights, bool OcrSucceeded, bool ThemeSucceeded, List<string> Errors);
}
