using ApprooveItApi.Models;
using ApprooveItApi.Helpers;

namespace ApprooveItApi.Repositories;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, string userId);
    Task<Submission?> GetByIdWithIncludesAsync(Guid id, string userId);
    Task<Submission?> GetByIdInternalAsync(Guid id);
    Task<Submission?> GetByTokenAsync(string token);
    Task<IEnumerable<Submission>> GetAllByUserIdAsync(string userId);
    Task<IEnumerable<Submission>> GetAllByUserIdAsync(string userId, SubmissionQueryObject query);
    Task<IEnumerable<Submission>> GetByClientIdAsync(Guid clientId, string userId);
    Task<Submission> CreateAsync(Submission submission);
    Task UpdateAsync(Submission submission);
    Task DeleteAsync(Submission submission);
    Task<bool> SaveChangesAsync();
}
