using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionResponse?> GetUserSubscriptionAsync(string userId);
    Task<UsageStats> GetUsageStatsAsync(string userId);
    Task<bool> ValidateSubmissionQuotaAsync(string userId);
    Task<bool> CheckFeatureAccessAsync(string userId, string feature);
    Task<SubscriptionTier> GetUserTierAsync(string userId);
    Task<List<PlanResponse>> GetAvailablePlansAsync();
    Task<CheckoutSessionResponse> CreateCheckoutAsync(string userId, CreateCheckoutRequest request);
    Task<bool> CancelSubscriptionAsync(string userId, CancelSubscriptionRequest request);
    Task<string> GetCustomerPortalUrlAsync(string userId);
    Task SyncSubscriptionFromPaddleAsync(string paddleSubscriptionId);
    Task<Subscription> CreateOrUpdateSubscriptionAsync(string userId, PaddleEventData data);
    Task HandleSubscriptionCancelledAsync(string paddleSubscriptionId, DateTime? cancelledAt);
    Task HandleSubscriptionPausedAsync(string paddleSubscriptionId, DateTime pausedAt);
    Task HandleSubscriptionResumedAsync(string paddleSubscriptionId);
    Task<Transaction> RecordTransactionAsync(string userId, PaddleEventData data);
}
