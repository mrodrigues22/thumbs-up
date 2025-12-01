using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id);
    Task<Subscription?> GetByUserIdAsync(string userId);
    Task<Subscription?> GetByPaddleSubscriptionIdAsync(string paddleSubscriptionId);
    Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status);
    Task<List<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate);
    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string userId);
}
