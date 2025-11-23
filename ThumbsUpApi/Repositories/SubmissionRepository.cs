using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public SubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Submission?> GetByIdAsync(Guid id, string userId)
    {
        return await _context.Submissions
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == userId);
    }

    public async Task<Submission?> GetByIdWithIncludesAsync(Guid id, string userId)
    {
        return await _context.Submissions
            .Include(s => s.MediaFiles)
            .Include(s => s.Review)
            .Include(s => s.ContentFeature)
            .Include(s => s.CreatedBy)
            .Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == userId);
    }

    public async Task<Submission?> GetByIdInternalAsync(Guid id)
    {
        return await _context.Submissions
            .Include(s => s.MediaFiles)
            .Include(s => s.ContentFeature)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Submission?> GetByTokenAsync(string token)
    {
        return await _context.Submissions
            .FirstOrDefaultAsync(s => s.AccessToken == token);
    }

    public async Task<IEnumerable<Submission>> GetAllByUserIdAsync(string userId)
    {
        return await _context.Submissions
            .Include(s => s.MediaFiles)
            .Include(s => s.Review)
            .Include(s => s.ContentFeature)
            .Include(s => s.CreatedBy)
            .Include(s => s.Client)
            .Where(s => s.CreatedById == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Submission>> GetByClientIdAsync(Guid clientId, string userId)
    {
        // First get the client to find their email
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null)
        {
            return Enumerable.Empty<Submission>();
        }

        // Match submissions by ClientId OR by ClientEmail (for submissions created before client entity)
        return await _context.Submissions
            .Include(s => s.MediaFiles)
            .Include(s => s.Review)
            .Include(s => s.ContentFeature)
            .Include(s => s.CreatedBy)
            .Include(s => s.Client)
            .Where(s => s.CreatedById == userId && 
                       (s.ClientId == clientId || s.ClientEmail == client.Email))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Submission> CreateAsync(Submission submission)
    {
        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync();
        return submission;
    }

    public async Task UpdateAsync(Submission submission)
    {
        _context.Submissions.Update(submission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Submission submission)
    {
        _context.Submissions.Remove(submission);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
