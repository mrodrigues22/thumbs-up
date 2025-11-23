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
        var submission = await _submissionRepo.GetByIdAsync(submissionId, userId);
        if (submission == null)
        {
            _logger.LogWarning("Submission {SubmissionId} not found for analysis", submissionId);
            return null;
        }

        var imageFiles = submission.MediaFiles.Where(m => m.FileType == MediaFileType.Image).ToList();
        if (imageFiles.Count == 0)
        {
            _logger.LogInformation("Submission {SubmissionId} has no image files for analysis", submissionId);
            return null; // For now skip video analysis
        }

        var allOcr = new List<string>();
        var tagSet = new HashSet<string>();

        foreach (var media in imageFiles)
        {
            try
            {
                var physical = _fileStorage.GetPhysicalPath(media.FilePath);
                var ocrText = await _ocr.ExtractTextAsync(physical, ct);
                if (!string.IsNullOrWhiteSpace(ocrText)) allOcr.Add(ocrText.Trim());

                var themes = await _themes.ExtractThemesAsync(physical, ct);
                foreach (var t in themes) tagSet.Add(t.Trim().ToLowerInvariant());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing media {MediaId} for submission {SubmissionId}", media.Id, submissionId);
            }
        }

        var feature = new ContentFeature
        {
            SubmissionId = submission.Id,
            OcrText = allOcr.Count == 0 ? null : string.Join("\n", allOcr),
            ThemeTagsJson = tagSet.Count == 0 ? null : JsonSerializer.Serialize(tagSet.OrderBy(t => t).ToList())
        };

        var persisted = await _featureRepo.UpsertAsync(feature);
        await _featureRepo.SaveChangesAsync();
        _logger.LogInformation("ContentFeature stored for submission {SubmissionId} with {TagCount} tags", submissionId, tagSet.Count);
        return persisted;
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

        var imageFiles = submission.MediaFiles.Where(m => m.FileType == MediaFileType.Image).ToList();
        if (imageFiles.Count == 0)
        {
            _logger.LogInformation("Submission {SubmissionId} has no image files for analysis", submissionId);
            return null;
        }

        var allOcr = new List<string>();
        var tagSet = new HashSet<string>();

        foreach (var media in imageFiles)
        {
            try
            {
                var physical = _fileStorage.GetPhysicalPath(media.FilePath);
                var ocrText = await _ocr.ExtractTextAsync(physical, ct);
                if (!string.IsNullOrWhiteSpace(ocrText)) allOcr.Add(ocrText.Trim());

                var themes = await _themes.ExtractThemesAsync(physical, ct);
                foreach (var t in themes) tagSet.Add(t.Trim().ToLowerInvariant());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing media {MediaId} for submission {SubmissionId}", media.Id, submissionId);
            }
        }

        var feature = new ContentFeature
        {
            SubmissionId = submission.Id,
            OcrText = allOcr.Count == 0 ? null : string.Join("\n", allOcr),
            ThemeTagsJson = tagSet.Count == 0 ? null : JsonSerializer.Serialize(tagSet.OrderBy(t => t).ToList())
        };

        var persisted = await _featureRepo.UpsertAsync(feature);
        await _featureRepo.SaveChangesAsync();
        _logger.LogInformation("ContentFeature stored for submission {SubmissionId} with {TagCount} tags", submissionId, tagSet.Count);
        return persisted;
    }
}
