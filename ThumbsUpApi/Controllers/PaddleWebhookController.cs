using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Controllers;

[ApiController]
[Route("api/paddle/webhook")]
[AllowAnonymous]
public class PaddleWebhookController : ControllerBase
{
    private readonly IPaddleService _paddleService;
    private readonly ISubscriptionLimitService _limitService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaddleWebhookController> _logger;

    public PaddleWebhookController(
        IPaddleService paddleService,
        ISubscriptionLimitService limitService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaddleWebhookController> logger)
    {
        _paddleService = paddleService;
        _limitService = limitService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Read raw request body
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var requestBody = await reader.ReadToEndAsync();

            // Validate webhook signature
            var signature = Request.Headers["Paddle-Signature"].ToString();
            if (!_paddleService.ValidateWebhookSignature(signature, requestBody))
            {
                _logger.LogWarning("Invalid webhook signature");
                return Unauthorized(new { message = "Invalid signature" });
            }

            // Parse webhook event
            var webhookEvent = JsonSerializer.Deserialize<PaddleWebhookEvent>(requestBody);
            if (webhookEvent == null)
            {
                _logger.LogWarning("Failed to parse webhook event");
                return BadRequest(new { message = "Invalid webhook data" });
            }

            _logger.LogInformation("Received Paddle webhook: {EventType}", webhookEvent.EventType);

            // Handle different event types
            await HandleWebhookEvent(webhookEvent);

            return Ok(new { message = "Webhook processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paddle webhook");
            return StatusCode(500, new { message = "Error processing webhook" });
        }
    }

    private async Task HandleWebhookEvent(PaddleWebhookEvent webhookEvent)
    {
        switch (webhookEvent.EventType)
        {
            case "subscription.created":
                await HandleSubscriptionCreated(webhookEvent.Data);
                break;

            case "subscription.updated":
                await HandleSubscriptionUpdated(webhookEvent.Data);
                break;

            case "subscription.cancelled":
                await HandleSubscriptionCancelled(webhookEvent.Data);
                break;

            case "subscription.payment_succeeded":
                await HandlePaymentSucceeded(webhookEvent.Data);
                break;

            case "subscription.payment_failed":
                await HandlePaymentFailed(webhookEvent.Data);
                break;

            case "subscription.activated":
                await HandleSubscriptionActivated(webhookEvent.Data);
                break;

            case "subscription.paused":
                await HandleSubscriptionPaused(webhookEvent.Data);
                break;

            case "subscription.past_due":
                await HandleSubscriptionPastDue(webhookEvent.Data);
                break;

            default:
                _logger.LogInformation("Unhandled webhook event type: {EventType}", webhookEvent.EventType);
                break;
        }
    }

    private async Task HandleSubscriptionCreated(PaddleEventData data)
    {
        var userId = data.UserId;
        if (string.IsNullOrEmpty(userId)) return;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for subscription creation: {UserId}", userId);
            return;
        }

        var tier = GetTierFromPriceId(data.Price?.Id);

        user.PaddleSubscriptionId = data.SubscriptionId;
        user.PaddleCustomerId = data.CustomerId;
        user.SubscriptionStatus = SubscriptionStatus.Active;
        user.SubscriptionTier = tier;
        user.SubscriptionStartDate = data.CurrentBillingPeriodStartsAt ?? DateTime.UtcNow;
        user.SubscriptionEndDate = data.CurrentBillingPeriodEndsAt;
        user.UsageResetDate = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Subscription created for user {UserId}, tier: {Tier}", userId, tier);
    }

    private async Task HandleSubscriptionUpdated(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null)
        {
            _logger.LogWarning("User not found for subscription update: {SubscriptionId}", subscriptionId);
            return;
        }

        var tier = GetTierFromPriceId(data.Price?.Id);

        user.SubscriptionTier = tier;
        user.SubscriptionEndDate = data.CurrentBillingPeriodEndsAt;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Subscription updated for user {UserId}, new tier: {Tier}", user.Id, tier);
    }

    private async Task HandleSubscriptionCancelled(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null)
        {
            _logger.LogWarning("User not found for subscription cancellation: {SubscriptionId}", subscriptionId);
            return;
        }

        user.SubscriptionStatus = SubscriptionStatus.Cancelled;
        user.SubscriptionCancelledAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Subscription cancelled for user {UserId}", user.Id);
    }

    private async Task HandlePaymentSucceeded(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null)
        {
            _logger.LogWarning("User not found for payment success: {SubscriptionId}", subscriptionId);
            return;
        }

        // Reset monthly usage counters on successful payment
        await _limitService.ResetMonthlyUsageAsync(user.Id);

        user.SubscriptionStatus = SubscriptionStatus.Active;
        user.SubscriptionEndDate = data.CurrentBillingPeriodEndsAt;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Payment succeeded for user {UserId}, usage reset", user.Id);
    }

    private async Task HandlePaymentFailed(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null)
        {
            _logger.LogWarning("User not found for payment failure: {SubscriptionId}", subscriptionId);
            return;
        }

        user.SubscriptionStatus = SubscriptionStatus.PastDue;

        await _userManager.UpdateAsync(user);

        _logger.LogWarning("Payment failed for user {UserId}", user.Id);
    }

    private async Task HandleSubscriptionActivated(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null) return;

        user.SubscriptionStatus = SubscriptionStatus.Active;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Subscription activated for user {UserId}", user.Id);
    }

    private async Task HandleSubscriptionPaused(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null) return;

        user.SubscriptionStatus = SubscriptionStatus.Cancelled;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Subscription paused for user {UserId}", user.Id);
    }

    private async Task HandleSubscriptionPastDue(PaddleEventData data)
    {
        var subscriptionId = data.SubscriptionId;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleSubscriptionId == subscriptionId);

        if (user == null) return;

        user.SubscriptionStatus = SubscriptionStatus.PastDue;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Subscription past due for user {UserId}", user.Id);
    }

    private SubscriptionTier GetTierFromPriceId(string? priceId)
    {
        if (string.IsNullOrEmpty(priceId)) return SubscriptionTier.None;

        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (priceId == config["Paddle:PriceIds:Starter"])
            return SubscriptionTier.Starter;
        if (priceId == config["Paddle:PriceIds:Pro"])
            return SubscriptionTier.Pro;
        if (priceId == config["Paddle:PriceIds:Enterprise"])
            return SubscriptionTier.Enterprise;

        return SubscriptionTier.None;
    }
}
