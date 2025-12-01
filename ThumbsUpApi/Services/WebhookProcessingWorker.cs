namespace ThumbsUpApi.Services;

/// <summary>
/// Background worker that processes Paddle webhook events from the queue
/// </summary>
public class WebhookProcessingWorker : BackgroundService
{
    private readonly IWebhookProcessingQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookProcessingWorker> _logger;

    public WebhookProcessingWorker(
        IWebhookProcessingQueue queue,
        IServiceProvider serviceProvider,
        ILogger<WebhookProcessingWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Processing Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var webhookEvent = await _queue.DequeueAsync(stoppingToken);

                // Process webhook in a new scope to get scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var webhookService = scope.ServiceProvider.GetRequiredService<PaddleWebhookService>();
                    await webhookService.HandleWebhookEventAsync(webhookEvent);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook event from queue");
                // Continue processing other events
            }
        }

        _logger.LogInformation("Webhook Processing Worker stopped");
    }
}
