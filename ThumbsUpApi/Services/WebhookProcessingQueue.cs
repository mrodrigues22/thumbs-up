using System.Threading.Channels;
using ThumbsUpApi.DTOs;

namespace ThumbsUpApi.Services;

/// <summary>
/// Background queue for processing Paddle webhook events asynchronously
/// </summary>
public interface IWebhookProcessingQueue
{
    ValueTask QueueWebhookEventAsync(PaddleWebhookEvent webhookEvent);
    ValueTask<PaddleWebhookEvent> DequeueAsync(CancellationToken cancellationToken);
}

public class WebhookProcessingQueue : IWebhookProcessingQueue
{
    private readonly Channel<PaddleWebhookEvent> _queue;

    public WebhookProcessingQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<PaddleWebhookEvent>(options);
    }

    public async ValueTask QueueWebhookEventAsync(PaddleWebhookEvent webhookEvent)
    {
        if (webhookEvent == null)
            throw new ArgumentNullException(nameof(webhookEvent));

        await _queue.Writer.WriteAsync(webhookEvent);
    }

    public async ValueTask<PaddleWebhookEvent> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
