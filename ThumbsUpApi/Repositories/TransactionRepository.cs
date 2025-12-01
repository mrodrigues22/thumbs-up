using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Subscription)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction?> GetByPaddleTransactionIdAsync(string paddleTransactionId)
    {
        return await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Subscription)
            .FirstOrDefaultAsync(t => t.PaddleTransactionId == paddleTransactionId);
    }

    public async Task<List<Transaction>> GetByUserIdAsync(string userId, int pageSize = 50, int page = 1)
    {
        return await _context.Transactions
            .Include(t => t.Subscription)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _context.Transactions
            .Where(t => t.SubscriptionId == subscriptionId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> ExistsAsync(string paddleTransactionId)
    {
        return await _context.Transactions.AnyAsync(t => t.PaddleTransactionId == paddleTransactionId);
    }
}
