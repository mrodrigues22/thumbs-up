using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public interface IContentFeatureRepository
{
    Task<ContentFeature?> GetBySubmissionIdAsync(Guid submissionId);
    Task<IEnumerable<ContentFeature>> GetBySubmissionIdsAsync(IEnumerable<Guid> submissionIds, CancellationToken ct = default);
    Task<ContentFeature> UpsertAsync(ContentFeature feature); // Creates or updates based on SubmissionId
    Task<bool> SaveChangesAsync();
}
