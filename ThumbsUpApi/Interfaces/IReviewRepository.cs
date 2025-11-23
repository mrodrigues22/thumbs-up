using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<Review?> GetBySubmissionIdAsync(Guid submissionId);
    Task<IEnumerable<Review>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);
    Task<Review> CreateAsync(Review review);
    Task UpdateAsync(Review review);
    Task<bool> SaveChangesAsync();
    Task<(int approved, int rejected, int total)> GetClientStatsAsync(Guid clientId, CancellationToken ct = default);
    Task<(int approved, int rejected, int total)> GetGlobalStatsAsync(CancellationToken ct = default);
}
