using ThumbsUpApi.DTOs;

namespace ThumbsUpApi.Interfaces;

public interface IPaddleService
{
    Task<CheckoutResponse> CreateCheckoutSessionAsync(string priceId, string userId, string? successUrl = null);
    Task<string> GetSubscriptionStatusAsync(string subscriptionId);
    Task CancelSubscriptionAsync(string subscriptionId, bool immediately = false);
    Task<string> UpdateSubscriptionAsync(string subscriptionId, string newPriceId);
    Task<string> GetUpdatePaymentMethodUrlAsync(string subscriptionId);
    bool ValidateWebhookSignature(string signature, string requestBody);
}
