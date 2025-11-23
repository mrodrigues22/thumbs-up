using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface IReviewPredictorService
{
    /// <summary>
    /// Gets or rebuilds a cached client summary with separate AI-generated insights.
    /// Rebuild conditions: no summary exists OR approved/rejected review count changed.
    /// </summary>
    Task<ClientSummaryResponse?> GetClientSummaryAsync(Guid clientId, string userId, CancellationToken ct = default);
}
