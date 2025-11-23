using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface IReviewPredictorService
{
    /// <summary>
    /// Gets or rebuilds a cached client summary. Rebuild conditions: no summary exists OR approved/rejected review count changed.
    /// </summary>
    Task<ClientSummary?> GetOrRefreshSummaryAsync(Guid clientId, string userId, CancellationToken ct = default);
}
