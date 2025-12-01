using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<Transaction?> GetByPaddleTransactionIdAsync(string paddleTransactionId);
    Task<List<Transaction>> GetByUserIdAsync(string userId, int pageSize = 50, int page = 1);
    Task<List<Transaction>> GetBySubscriptionIdAsync(Guid subscriptionId);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction> UpdateAsync(Transaction transaction);
    Task<bool> ExistsAsync(string paddleTransactionId);
}
