using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<Review?> GetBySubmissionIdAsync(Guid submissionId);
    Task<Review> CreateAsync(Review review);
    Task UpdateAsync(Review review);
    Task<bool> SaveChangesAsync();
}
