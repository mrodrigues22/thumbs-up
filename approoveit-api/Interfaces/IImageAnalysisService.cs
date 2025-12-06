using ApprooveItApi.Models;

namespace ApprooveItApi.Interfaces;

public interface IImageAnalysisService
{
    /// <summary>
    /// Runs OCR + theme extraction for image media files of a submission and persists aggregate feature.
    /// Safe to call multiple times; it upserts the feature row.
    /// </summary>
    Task<ContentFeature?> AnalyzeSubmissionAsync(Guid submissionId, CancellationToken ct = default);
}
