using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;
using ThumbsUpApi.Helpers;

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
            .Include(s => s.MediaFiles.OrderBy(m => m.Order))
            .Include(s => s.Review)
            .Include(s => s.ContentFeature)
            .Include(s => s.CreatedBy)
            .Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == userId);
    }

    public async Task<Submission?> GetByIdInternalAsync(Guid id)
    {
        return await _context.Submissions
            .Include(s => s.MediaFiles.OrderBy(m => m.Order))
            .Include(s => s.ContentFeature)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Submission?> GetByTokenAsync(string token)
    {
        return await _context.Submissions
            .FirstOrDefaultAsync(s => s.AccessToken.ToLower() == token.ToLower());
    }

    public async Task<IEnumerable<Submission>> GetAllByUserIdAsync(string userId)
    {
        return await _context.Submissions
            .Include(s => s.MediaFiles.OrderBy(m => m.Order))
            .Include(s => s.Review)
            .Include(s => s.ContentFeature)
            .Include(s => s.CreatedBy)
            .Include(s => s.Client)
            .Where(s => s.CreatedById == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Submission>> GetAllByUserIdAsync(string userId, SubmissionQueryObject query)
    {
        var queryable = _context.Submissions
            .Include(s => s.MediaFiles.OrderBy(m => m.Order))
            .Include(s => s.Review)
            .Include(s => s.ContentFeature)
            .Include(s => s.CreatedBy)
            .Include(s => s.Client)
            .Where(s => s.CreatedById == userId)
            .AsQueryable();

        // Apply status filter
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        }

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            queryable = queryable.Where(s => 
                (s.Client != null && s.Client.Name.ToLower().Contains(searchLower)) ||
                s.ClientEmail.ToLower().Contains(searchLower) ||
                (s.Captions != null && s.Captions.ToLower().Contains(searchLower)) ||
                (s.Message != null && s.Message.ToLower().Contains(searchLower))
            );
        }

        // Apply date range filters
        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(s => s.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            // Add one day to include the entire end date
            var dateTo = query.DateTo.Value.AddDays(1);
            queryable = queryable.Where(s => s.CreatedAt < dateTo);
        }

        // Apply sorting
        var sortBy = query.SortBy?.ToLower() ?? "createdat";
        var sortOrder = query.SortOrder?.ToLower() ?? "desc";

        queryable = sortBy switch
        {
            "client" => sortOrder == "asc" 
                ? queryable.OrderBy(s => s.Client != null ? s.Client.Name : s.ClientEmail)
                : queryable.OrderByDescending(s => s.Client != null ? s.Client.Name : s.ClientEmail),
            "status" => sortOrder == "asc"
                ? queryable.OrderBy(s => s.Status)
                : queryable.OrderByDescending(s => s.Status),
            _ => sortOrder == "asc"
                ? queryable.OrderBy(s => s.CreatedAt)
                : queryable.OrderByDescending(s => s.CreatedAt)
        };

        return await queryable.ToListAsync();
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
            .Include(s => s.MediaFiles.OrderBy(m => m.Order))
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
