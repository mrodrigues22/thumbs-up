using ThumbsUpApi.DTOs;

namespace ThumbsUpApi.Interfaces;

public interface IPaddleService
{
    Task<string> CreateCustomerAsync(string email, string name);
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string customerId, 
        string priceId, 
        string? successUrl = null, 
        string? cancelUrl = null);
    Task<PaddleEventData> GetSubscriptionAsync(string subscriptionId);
    Task<bool> CancelSubscriptionAsync(string subscriptionId, bool immediately = false);
    Task<bool> PauseSubscriptionAsync(string subscriptionId);
    Task<bool> ResumeSubscriptionAsync(string subscriptionId);
    Task<string> GetCustomerPortalUrlAsync(string customerId);
    bool VerifyWebhookSignature(string signature, string requestBody);
}
