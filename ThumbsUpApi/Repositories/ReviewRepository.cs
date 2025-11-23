using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(Guid id)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Review?> GetBySubmissionIdAsync(Guid submissionId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.SubmissionId == submissionId);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task UpdateAsync(Review review)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<(int approved, int rejected, int total)> GetClientStatsAsync(Guid clientId, CancellationToken ct = default)
    {
        var clientSubmissionIds = await _context.Submissions
            .Where(s => s.ClientId == clientId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var clientReviews = await _context.Reviews
            .Where(r => clientSubmissionIds.Contains(r.SubmissionId))
            .ToListAsync(ct);

        var approved = clientReviews.Count(r => r.Status == ReviewStatus.Approved);
        var rejected = clientReviews.Count(r => r.Status == ReviewStatus.Rejected);
        var total = approved + rejected;
        return (approved, rejected, total);
    }

    public async Task<(int approved, int rejected, int total)> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        var approved = await _context.Reviews.CountAsync(r => r.Status == ReviewStatus.Approved, ct);
        var rejected = await _context.Reviews.CountAsync(r => r.Status == ReviewStatus.Rejected, ct);
        var total = approved + rejected;
        return (approved, rejected, total);
    }
}
