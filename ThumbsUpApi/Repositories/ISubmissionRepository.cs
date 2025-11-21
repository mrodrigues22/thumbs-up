using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, string userId);
    Task<Submission?> GetByIdWithIncludesAsync(Guid id, string userId);
    Task<Submission?> GetByTokenAsync(string token);
    Task<Submission?> GetByTokenWithIncludesAsync(string token);
    Task<IEnumerable<Submission>> GetAllByUserIdAsync(string userId);
    Task<Submission> CreateAsync(Submission submission);
    Task UpdateAsync(Submission submission);
    Task DeleteAsync(Submission submission);
    Task<bool> SaveChangesAsync();
}
