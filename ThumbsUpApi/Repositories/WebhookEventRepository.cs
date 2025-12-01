using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Data;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Repositories;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly ApplicationDbContext _context;

    public WebhookEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsEventProcessedAsync(string eventId)
    {
        return await _context.ProcessedWebhookEvents
            .AnyAsync(e => e.EventId == eventId);
    }

    public async Task MarkEventAsProcessedAsync(string eventId, string eventType, DateTime occurredAt, string? subscriptionId = null, string? customerId = null)
    {
        var processedEvent = new ProcessedWebhookEvent
        {
            EventId = eventId,
            EventType = eventType,
            OccurredAt = occurredAt,
            SubscriptionId = subscriptionId,
            CustomerId = customerId
        };

        _context.ProcessedWebhookEvents.Add(processedEvent);
        await _context.SaveChangesAsync();
    }
}
