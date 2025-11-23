using ThumbsUpApi.DTOs;

namespace ThumbsUpApi.Interfaces;

public interface IContentSummaryService
{
    /// <summary>
    /// Generates a content summary for a submission based on its media files and extracted features.
    /// </summary>
    /// <param name="submission">The submission response containing media files and content features</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A human-readable summary of the submission content</returns>
    Task<string> GenerateAsync(SubmissionResponse submission, CancellationToken cancellationToken = default);
}
