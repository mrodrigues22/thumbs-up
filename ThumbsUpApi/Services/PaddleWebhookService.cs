using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

public class PaddleWebhookService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IWebhookEventRepository _webhookEventRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaddleWebhookService> _logger;

    public PaddleWebhookService(
        ISubscriptionService subscriptionService,
        IWebhookEventRepository webhookEventRepo,
        UserManager<ApplicationUser> userManager,
        ILogger<PaddleWebhookService> logger)
    {
        _subscriptionService = subscriptionService;
        _webhookEventRepo = webhookEventRepo;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task HandleWebhookEventAsync(PaddleWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Processing Paddle webhook: {EventType} - {EventId}", 
            webhookEvent.EventType, webhookEvent.EventId);

        try
        {
            switch (webhookEvent.EventType)
            {
                case "subscription.created":
                    await HandleSubscriptionCreatedAsync(webhookEvent.Data);
                    break;

                case "subscription.updated":
                    await HandleSubscriptionUpdatedAsync(webhookEvent.Data);
                    break;

                case "subscription.canceled":
                case "subscription.cancelled":
                    await HandleSubscriptionCancelledAsync(webhookEvent.Data);
                    break;

                case "subscription.paused":
                    await HandleSubscriptionPausedAsync(webhookEvent.Data);
                    break;

                case "subscription.resumed":
                    await HandleSubscriptionResumedAsync(webhookEvent.Data);
                    break;

                case "subscription.past_due":
                    await HandleSubscriptionPastDueAsync(webhookEvent.Data);
                    break;

                case "transaction.completed":
                    await HandleTransactionCompletedAsync(webhookEvent.Data);
                    break;

                case "transaction.payment_failed":
                    await HandleTransactionFailedAsync(webhookEvent.Data);
                    break;

                case "customer.updated":
                    await HandleCustomerUpdatedAsync(webhookEvent.Data);
                    break;

                default:
                    _logger.LogWarning("Unhandled webhook event type: {EventType}", webhookEvent.EventType);
                    break;
            }

            // Mark event as processed after successful handling
            await _webhookEventRepo.MarkEventAsProcessedAsync(
                webhookEvent.EventId,
                webhookEvent.EventType,
                webhookEvent.OccurredAt,
                webhookEvent.Data.SubscriptionId ?? webhookEvent.Data.Id,
                webhookEvent.Data.CustomerId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType} - {EventId}", 
                webhookEvent.EventType, webhookEvent.EventId);
            throw;
        }
    }

    private async Task HandleSubscriptionCreatedAsync(PaddleEventData data)
    {
        if (string.IsNullOrEmpty(data.CustomerId))
        {
            _logger.LogWarning("Subscription created without customer ID: {SubscriptionId}", data.Id);
            return;
        }

        // Find user by Paddle customer ID
        var userId = await FindUserIdByPaddleCustomerIdAsync(data.CustomerId);
        if (userId == null)
        {
            _logger.LogWarning("Could not find user for Paddle customer: {CustomerId}", data.CustomerId);
            return;
        }

        await _subscriptionService.CreateOrUpdateSubscriptionAsync(userId, data);
        _logger.LogInformation("Subscription created for user {UserId}: {SubscriptionId}", userId, data.Id);
    }

    private async Task HandleSubscriptionUpdatedAsync(PaddleEventData data)
    {
        if (string.IsNullOrEmpty(data.CustomerId))
            return;

        var userId = await FindUserIdByPaddleCustomerIdAsync(data.CustomerId);
        if (userId == null)
            return;

        await _subscriptionService.CreateOrUpdateSubscriptionAsync(userId, data);
        _logger.LogInformation("Subscription updated for user {UserId}: {SubscriptionId}", userId, data.Id);
    }

    private async Task HandleSubscriptionCancelledAsync(PaddleEventData data)
    {
        await _subscriptionService.HandleSubscriptionCancelledAsync(data.Id, data.CanceledAt);
        _logger.LogInformation("Subscription cancelled: {SubscriptionId}", data.Id);
    }

    private async Task HandleSubscriptionPausedAsync(PaddleEventData data)
    {
        await _subscriptionService.HandleSubscriptionPausedAsync(data.Id, data.PausedAt ?? DateTime.UtcNow);
        _logger.LogInformation("Subscription paused: {SubscriptionId}", data.Id);
    }

    private async Task HandleSubscriptionResumedAsync(PaddleEventData data)
    {
        await _subscriptionService.HandleSubscriptionResumedAsync(data.Id);
        _logger.LogInformation("Subscription resumed: {SubscriptionId}", data.Id);
    }

    private async Task HandleSubscriptionPastDueAsync(PaddleEventData data)
    {
        if (string.IsNullOrEmpty(data.CustomerId))
            return;

        var userId = await FindUserIdByPaddleCustomerIdAsync(data.CustomerId);
        if (userId == null)
            return;

        await _subscriptionService.CreateOrUpdateSubscriptionAsync(userId, data);
        _logger.LogWarning("Subscription past due for user {UserId}: {SubscriptionId}", userId, data.Id);
    }

    private async Task HandleTransactionCompletedAsync(PaddleEventData data)
    {
        if (string.IsNullOrEmpty(data.CustomerId))
            return;

        var userId = await FindUserIdByPaddleCustomerIdAsync(data.CustomerId);
        if (userId == null)
            return;

        await _subscriptionService.RecordTransactionAsync(userId, data);
        _logger.LogInformation("Transaction completed for user {UserId}: {TransactionId}", userId, data.Id);
    }

    private async Task HandleTransactionFailedAsync(PaddleEventData data)
    {
        if (string.IsNullOrEmpty(data.CustomerId))
            return;

        var userId = await FindUserIdByPaddleCustomerIdAsync(data.CustomerId);
        if (userId == null)
            return;

        await _subscriptionService.RecordTransactionAsync(userId, data);
        _logger.LogWarning("Transaction failed for user {UserId}: {TransactionId}", userId, data.Id);
    }

    private async Task HandleCustomerUpdatedAsync(PaddleEventData data)
    {
        if (string.IsNullOrEmpty(data.Id))
            return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleCustomerId == data.Id);
        
        if (user == null)
        {
            _logger.LogWarning("Could not find user for Paddle customer: {CustomerId}", data.Id);
            return;
        }

        // Update user email if changed
        if (!string.IsNullOrEmpty(data.Customer?.Email) && user.Email != data.Customer.Email)
        {
            _logger.LogInformation("Updating email for user {UserId} from {OldEmail} to {NewEmail}",
                user.Id, user.Email, data.Customer.Email);
            user.Email = data.Customer.Email;
            user.NormalizedEmail = data.Customer.Email.ToUpper();
            await _userManager.UpdateAsync(user);
        }
    }

    private async Task<string?> FindUserIdByPaddleCustomerIdAsync(string paddleCustomerId)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PaddleCustomerId == paddleCustomerId);
        return user?.Id;
    }
}
