using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/paddle/webhook")]
[AllowAnonymous]
public class PaddleWebhookController : ControllerBase
{
    private readonly IPaddleService _paddleService;
    private readonly PaddleWebhookService _webhookService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaddleWebhookController> _logger;

    public PaddleWebhookController(
        IPaddleService paddleService,
        PaddleWebhookService webhookService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaddleWebhookController> logger)
    {
        _paddleService = paddleService;
        _webhookService = webhookService;
        _userManager = userManager;
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

            // Process webhook asynchronously (fire and forget pattern for production)
            // For now, we'll await it for better error handling
            await _webhookService.HandleWebhookEventAsync(webhookEvent);

            return Ok(new { message = "Webhook processed successfully" });
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
