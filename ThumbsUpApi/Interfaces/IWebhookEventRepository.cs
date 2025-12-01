using ThumbsUpApi.Models;

namespace ThumbsUpApi.Interfaces;

public interface IWebhookEventRepository
{
    Task<bool> IsEventProcessedAsync(string eventId);
    Task MarkEventAsProcessedAsync(string eventId, string eventType, DateTime occurredAt, string? subscriptionId = null, string? customerId = null);
}
