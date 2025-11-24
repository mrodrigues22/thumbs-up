using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Mappers;

public class SubmissionMapper
{
    private readonly IFileStorageService _fileStorage;

    public SubmissionMapper(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public SubmissionResponse ToResponse(Submission submission)
    {
        return new SubmissionResponse
        {
            Id = submission.Id,
            ClientId = submission.ClientId,
            ClientEmail = submission.ClientEmail,
            ClientName = submission.Client?.Name,
            ClientCompanyName = submission.Client?.CompanyName,
            AccessToken = submission.AccessToken,
            AccessPassword = submission.AccessPassword,
            Message = submission.Message,
            Captions = submission.Captions,
            Status = submission.Status,
            CreatedAt = submission.CreatedAt,
            ExpiresAt = submission.ExpiresAt,
            MediaFiles = submission.MediaFiles?.Select(ToMediaFileResponse).ToList() ?? new List<MediaFileResponse>(),
            Review = submission.Review != null ? ToReviewResponse(submission.Review) : null,
            ContentFeature = submission.ContentFeature != null ? ToContentFeatureResponse(submission.ContentFeature) : null
        };
    }

    public MediaFileResponse ToMediaFileResponse(MediaFile mediaFile)
    {
        return new MediaFileResponse
        {
            Id = mediaFile.Id,
            FileName = mediaFile.FileName,
            FileUrl = _fileStorage.GetFileUrl(mediaFile.FilePath),
            FileType = mediaFile.FileType,
            FileSize = mediaFile.FileSize,
            UploadedAt = mediaFile.UploadedAt
        };
    }

    private ReviewResponse ToReviewResponse(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            Status = review.Status,
            Comment = review.Comment,
            ReviewedAt = review.ReviewedAt
        };
    }

    private ContentFeatureResponse ToContentFeatureResponse(ContentFeature feature)
    {
        var insights = ThemeInsights.FromJson(feature.ThemeTagsJson);
        var tags = insights
            .FlattenTags()
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag)
            .ToList();

        return new ContentFeatureResponse
        {
            OcrText = feature.OcrText,
            Tags = tags,
            ThemeInsights = BuildThemeInsightsResponse(insights),
            ExtractedAt = feature.ExtractedAt,
            LastAnalyzedAt = feature.LastAnalyzedAt,
            AnalysisStatus = feature.AnalysisStatus,
            FailureReason = feature.FailureReason
        };
    }

    private static ThemeInsightsResponse? BuildThemeInsightsResponse(ThemeInsights insights)
    {
        if (!insights.HasAnyData)
        {
            return null;
        }

        return new ThemeInsightsResponse
        {
            Subjects = new List<string>(insights.Subjects),
            Vibes = new List<string>(insights.Vibes),
            NotableElements = new List<string>(insights.NotableElements),
            Colors = new List<string>(insights.Colors),
            Keywords = new List<string>(insights.Keywords)
        };
    }
}
