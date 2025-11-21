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
            ClientEmail = submission.ClientEmail,
            AccessToken = submission.AccessToken,
            Message = submission.Message,
            Status = submission.Status,
            CreatedAt = submission.CreatedAt,
            ExpiresAt = submission.ExpiresAt,
            MediaFiles = submission.MediaFiles?.Select(ToMediaFileResponse).ToList() ?? new List<MediaFileResponse>(),
            Review = submission.Review != null ? ToReviewResponse(submission.Review) : null
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
}
