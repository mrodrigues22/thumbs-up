using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class ContentFeatureRepository : IContentFeatureRepository
{
    private readonly ApplicationDbContext _db;
    public ContentFeatureRepository(ApplicationDbContext db) => _db = db;

    public async Task<ContentFeature?> GetBySubmissionIdAsync(Guid submissionId)
    {
        return await _db.ContentFeatures.AsNoTracking().FirstOrDefaultAsync(f => f.SubmissionId == submissionId);
    }

    public async Task<IEnumerable<ContentFeature>> GetBySubmissionIdsAsync(IEnumerable<Guid> submissionIds, CancellationToken ct = default)
    {
        return await _db.ContentFeatures
            .AsNoTracking()
            .Where(f => submissionIds.Contains(f.SubmissionId))
            .ToListAsync(ct);
    }

    public async Task<ContentFeature> UpsertAsync(ContentFeature feature)
    {
        var existing = await _db.ContentFeatures.FirstOrDefaultAsync(f => f.SubmissionId == feature.SubmissionId);
        if (existing == null)
        {
            feature.Id = Guid.NewGuid();
            _db.ContentFeatures.Add(feature);
            return feature;
        }

        existing.OcrText = feature.OcrText;
        existing.ThemeTagsJson = feature.ThemeTagsJson;
        existing.ExtractedAt = DateTime.UtcNow;
        return existing;
    }

    public async Task<bool> SaveChangesAsync() => (await _db.SaveChangesAsync()) > 0;
}
