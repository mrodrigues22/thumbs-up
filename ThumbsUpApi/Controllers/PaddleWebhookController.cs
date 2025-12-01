using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/paddle/webhook")]
[AllowAnonymous]
[EnableRateLimiting("webhook")]
public class PaddleWebhookController : ControllerBase
{
    private readonly IPaddleService _paddleService;
    private readonly IWebhookProcessingQueue _webhookQueue;
    private readonly IWebhookEventRepository _webhookEventRepo;
    private readonly ILogger<PaddleWebhookController> _logger;

    public PaddleWebhookController(
        IPaddleService paddleService,
        IWebhookProcessingQueue webhookQueue,
        IWebhookEventRepository webhookEventRepo,
        ILogger<PaddleWebhookController> logger)
    {
        _paddleService = paddleService;
        _webhookQueue = webhookQueue;
        _webhookEventRepo = webhookEventRepo;
        _logger = logger;
    }

    /// <summary>
    /// Handle Paddle webhook events
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Read the raw request body
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            // Verify webhook signature
            var signature = Request.Headers["Paddle-Signature"].ToString();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook received without signature");
                return BadRequest(new { message = "Missing webhook signature" });
            }

            if (!_paddleService.VerifyWebhookSignature(signature, requestBody))
            {
                _logger.LogWarning("Webhook signature verification failed");
                return Unauthorized(new { message = "Invalid webhook signature" });
            }

            // Parse webhook event
            var webhookEvent = JsonSerializer.Deserialize<PaddleWebhookEvent>(
                requestBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (webhookEvent == null)
            {
                _logger.LogWarning("Failed to parse webhook event");
                return BadRequest(new { message = "Invalid webhook payload" });
            }

            _logger.LogInformation("Received Paddle webhook: {EventType} - {EventId}", 
                webhookEvent.EventType, webhookEvent.EventId);

            // Check if webhook event has already been processed (idempotency)
            if (await _webhookEventRepo.IsEventProcessedAsync(webhookEvent.EventId))
            {
                _logger.LogInformation("Webhook event {EventId} already processed, skipping", webhookEvent.EventId);
                return Ok(new { message = "Webhook event already processed" });
            }

            // Queue webhook for async processing
            await _webhookQueue.QueueWebhookEventAsync(webhookEvent);

            _logger.LogInformation("Webhook event {EventId} queued for processing", webhookEvent.EventId);
            return Ok(new { message = "Webhook queued for processing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paddle webhook");
            // Return 200 to prevent Paddle from retrying on application errors
            // Log the error for investigation
            return Ok(new { message = "Webhook received but processing failed" });
        }
    }

    /// <summary>
    /// Test endpoint to verify webhook configuration (development only)
    /// </summary>
    [HttpGet("test")]
    public IActionResult TestWebhook()
    {
        return Ok(new 
        { 
            message = "Paddle webhook endpoint is active",
            endpoint = "/api/paddle/webhook",
            timestamp = DateTime.UtcNow
        });
    }
}
