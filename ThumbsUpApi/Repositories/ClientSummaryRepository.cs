using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class ClientSummaryRepository : IClientSummaryRepository
{
    private readonly ApplicationDbContext _db;
    public ClientSummaryRepository(ApplicationDbContext db) => _db = db;

    public async Task<ClientSummary?> GetByClientIdAsync(Guid clientId)
    {
        return await _db.ClientSummaries.AsNoTracking().FirstOrDefaultAsync(s => s.ClientId == clientId);
    }

    public async Task<ClientSummary> UpsertAsync(ClientSummary summary)
    {
        var existing = await _db.ClientSummaries.FirstOrDefaultAsync(s => s.ClientId == summary.ClientId);
        if (existing == null)
        {
            summary.Id = Guid.NewGuid();
            _db.ClientSummaries.Add(summary);
            return summary;
        }

        existing.SummaryText = summary.SummaryText;
        existing.UpdatedAt = DateTime.UtcNow;
        return existing;
    }

    public async Task<bool> SaveChangesAsync() => (await _db.SaveChangesAsync()) > 0;
}
