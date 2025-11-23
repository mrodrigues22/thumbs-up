namespace ThumbsUpApi.Services;

public interface IApprovalPredictor
{
    /// <summary>
    /// Computes probability (0-1) that a submission would be approved by a specific client, plus rationale.
    /// </summary>
    Task<(double probability, string rationale)> PredictApprovalAsync(Guid clientId, Guid submissionId, string userId, CancellationToken ct = default);
}