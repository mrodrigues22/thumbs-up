using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subscription?> GetByUserIdAsync(string userId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Subscription?> GetByPaddleSubscriptionIdAsync(string paddleSubscriptionId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.PaddleSubscriptionId == paddleSubscriptionId);
    }

    public async Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => s.Status == status)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => s.CurrentPeriodEnd.HasValue && 
                       s.CurrentPeriodEnd.Value <= beforeDate &&
                       s.Status == SubscriptionStatus.Active)
            .ToListAsync();
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<Subscription> UpdateAsync(Subscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task DeleteAsync(Guid id)
    {
        var subscription = await _context.Subscriptions.FindAsync(id);
        if (subscription != null)
        {
            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        return await _context.Subscriptions.AnyAsync(s => s.UserId == userId);
    }
}
