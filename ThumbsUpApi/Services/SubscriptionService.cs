using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThumbsUpApi.Configuration;
using ThumbsUpApi.Data;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IPaddleService _paddleService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PaddleOptions _paddleOptions;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepo,
        ITransactionRepository transactionRepo,
        IPaddleService paddleService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IConfiguration configuration,
        IOptions<PaddleOptions> paddleOptions,
        ILogger<SubscriptionService> logger)
    {
        _subscriptionRepo = subscriptionRepo;
        _transactionRepo = transactionRepo;
        _paddleService = paddleService;
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _paddleOptions = paddleOptions.Value;
        _logger = logger;
    }

    public async Task<SubscriptionResponse?> GetUserSubscriptionAsync(string userId)
    {
        var subscription = await _subscriptionRepo.GetByUserIdAsync(userId);
        
        // No free tier - users must subscribe to use the service
        if (subscription == null)
        {
            return null;
        }

        var usage = await GetUsageStatsAsync(userId);
        var limits = GetLimitsForTier(subscription.Tier);

        return new SubscriptionResponse
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            Status = subscription.Status,
            Tier = subscription.Tier,
            PlanName = subscription.PlanName,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelledAt = subscription.CancelledAt,
            TrialEndsAt = subscription.TrialEndsAt,
            IsActive = subscription.Status == SubscriptionStatus.Active || 
                      subscription.Status == SubscriptionStatus.Trialing,
            CanUpgrade = subscription.Tier != SubscriptionTier.Enterprise,
            Limits = limits,
            Usage = usage
        };
    }

    public async Task<UsageStats> GetUsageStatsAsync(string userId)
    {
        var subscription = await _subscriptionRepo.GetByUserIdAsync(userId);
        if (subscription == null)
        {
            throw new Exception("No active subscription found. Please subscribe to a plan.");
        }
        var tier = subscription.Tier;
        var limits = GetLimitsForTier(tier);

        var periodStart = subscription?.CurrentPeriodStart ?? DateTime.UtcNow.Date;
        var periodEnd = subscription?.CurrentPeriodEnd ?? DateTime.UtcNow.AddMonths(1).Date;

        // Count submissions in current period
        var submissionsUsed = await _context.Submissions
            .Where(s => s.CreatedById == userId && 
                       s.CreatedAt >= periodStart && 
                       s.CreatedAt < periodEnd)
            .CountAsync();

        // Calculate storage used (in GB)
        var totalBytes = await _context.MediaFiles
            .Where(m => m.Submission.CreatedById == userId)
            .SumAsync(m => m.FileSize);
        var storageUsedGB = (decimal)totalBytes / (1024m * 1024m * 1024m);

        // Count clients
        var clientsCount = await _context.Clients
            .Where(c => c.CreatedById == userId)
            .CountAsync();

        var submissionsRemaining = limits.SubmissionsPerMonth == -1 
            ? int.MaxValue 
            : Math.Max(0, limits.SubmissionsPerMonth - submissionsUsed);

        var storageRemainingGB = limits.StorageGB == -1 
            ? decimal.MaxValue 
            : Math.Max(0, limits.StorageGB - storageUsedGB);

        var clientsRemaining = limits.ClientsMax == -1 
            ? int.MaxValue 
            : Math.Max(0, limits.ClientsMax - clientsCount);

        return new UsageStats
        {
            SubmissionsUsed = submissionsUsed,
            SubmissionsRemaining = submissionsRemaining,
            StorageUsedGB = Math.Round(storageUsedGB, 2),
            StorageRemainingGB = Math.Round(storageRemainingGB, 2),
            ClientsCount = clientsCount,
            ClientsRemaining = clientsRemaining,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };
    }

    public async Task<bool> ValidateSubmissionQuotaAsync(string userId)
    {
        var usage = await GetUsageStatsAsync(userId);
        return usage.SubmissionsRemaining > 0;
    }

    public async Task<bool> CheckFeatureAccessAsync(string userId, string feature)
    {
        var tier = await GetUserTierAsync(userId);
        var limits = GetLimitsForTier(tier);

        return feature.ToLower() switch
        {
            "ai" => limits.AiFeatures,
            "ai_features" => limits.AiFeatures,
            "priority_support" => limits.PrioritySupport,
            _ => true
        };
    }

    public async Task<SubscriptionTier> GetUserTierAsync(string userId)
    {
        var subscription = await _subscriptionRepo.GetByUserIdAsync(userId);
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
        {
            throw new Exception("No active subscription found. Please subscribe to a plan.");
        }
        return subscription.Tier;
    }

    public async Task<List<PlanResponse>> GetAvailablePlansAsync()
    {
        return new List<PlanResponse>
        {
            new PlanResponse
            {
                Id = _paddleOptions.PriceIds.GetValueOrDefault("Starter_Monthly", ""),
                Name = "Starter",
                Tier = SubscriptionTier.Starter,
                Description = "Perfect for individual professionals",
                MonthlyPrice = 12,
                YearlyPrice = 120,
                Currency = "USD",
                Features = new List<string>
                {
                    "20 submissions per month",
                    "5GB storage",
                    "Up to 10 clients",
                    "14-day submission expiry",
                    "Email support"
                },
                Limits = GetLimitsForTier(SubscriptionTier.Starter),
                IsPopular = false
            },
            new PlanResponse
            {
                Id = _paddleOptions.PriceIds.GetValueOrDefault("Pro_Monthly", ""),
                Name = "Pro",
                Tier = SubscriptionTier.Pro,
                Description = "For professionals managing client approvals",
                MonthlyPrice = 29,
                YearlyPrice = 290,
                Currency = "USD",
                Features = new List<string>
                {
                    "50 submissions per month",
                    "25GB storage",
                    "Unlimited clients",
                    "AI-powered insights",
                    "30-day submission expiry",
                    "Priority email support",
                    "Custom branding"
                },
                Limits = GetLimitsForTier(SubscriptionTier.Pro),
                IsPopular = true
            },
            new PlanResponse
            {
                Id = _paddleOptions.PriceIds.GetValueOrDefault("Enterprise_Monthly", ""),
                Name = "Enterprise",
                Tier = SubscriptionTier.Enterprise,
                Description = "For teams and agencies",
                MonthlyPrice = 99,
                YearlyPrice = 990,
                Currency = "USD",
                Features = new List<string>
                {
                    "Unlimited submissions",
                    "100GB storage",
                    "Unlimited clients",
                    "All AI features",
                    "90-day submission expiry",
                    "Dedicated account manager",
                    "White-label options",
                    "API access",
                    "Advanced analytics"
                },
                Limits = GetLimitsForTier(SubscriptionTier.Enterprise),
                IsPopular = false
            }
        };
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutAsync(string userId, CreateCheckoutRequest request)
    {
        _logger.LogInformation("Creating checkout for user {UserId} with price {PriceId}", userId, request.PriceId);
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            throw new Exception("User not found");
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("User {UserId} has no email", userId);
            throw new Exception("User email is required for subscription");
        }

        // Create Paddle customer if not exists
        if (string.IsNullOrEmpty(user.PaddleCustomerId))
        {
            _logger.LogInformation("Creating Paddle customer for user {UserId}", userId);
            try
            {
                var customerId = await _paddleService.CreateCustomerAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}".Trim()
                );
                user.PaddleCustomerId = customerId;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Paddle customer created: {CustomerId}", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Paddle customer for user {UserId}", userId);
                throw new Exception("Failed to create customer account. Please try again.", ex);
            }
        }

        // Validate price ID
        if (string.IsNullOrEmpty(request.PriceId))
        {
            _logger.LogWarning("Empty price ID provided");
            throw new Exception("Invalid price selection");
        }

        // Create checkout session
        try
        {
            return await _paddleService.CreateCheckoutSessionAsync(
                user.PaddleCustomerId,
                request.PriceId,
                request.SuccessUrl,
                request.CancelUrl
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for user {UserId}", userId);
            throw new Exception("Failed to create checkout session. Please try again or contact support.", ex);
        }
    }

    public async Task<bool> CancelSubscriptionAsync(string userId, CancelSubscriptionRequest request)
    {
        var subscription = await _subscriptionRepo.GetByUserIdAsync(userId);
        if (subscription == null || string.IsNullOrEmpty(subscription.PaddleSubscriptionId))
            return false;

        return await _paddleService.CancelSubscriptionAsync(
            subscription.PaddleSubscriptionId,
            request.Immediately
        );
    }

    public async Task<string> GetCustomerPortalUrlAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PaddleCustomerId))
            throw new Exception("User has no Paddle customer ID");

        return await _paddleService.GetCustomerPortalUrlAsync(user.PaddleCustomerId);
    }

    public async Task SyncSubscriptionFromPaddleAsync(string paddleSubscriptionId)
    {
        var paddleData = await _paddleService.GetSubscriptionAsync(paddleSubscriptionId);
        var subscription = await _subscriptionRepo.GetByPaddleSubscriptionIdAsync(paddleSubscriptionId);
        
        if (subscription != null)
        {
            UpdateSubscriptionFromPaddleData(subscription, paddleData);
            await _subscriptionRepo.UpdateAsync(subscription);
        }
    }

    public async Task<Subscription> CreateOrUpdateSubscriptionAsync(string userId, PaddleEventData data)
    {
        var subscription = await _subscriptionRepo.GetByUserIdAsync(userId);

        if (subscription == null)
        {
            subscription = new Subscription
            {
                UserId = userId,
                PaddleSubscriptionId = data.Id,
                PaddleCustomerId = data.CustomerId
            };
        }

        UpdateSubscriptionFromPaddleData(subscription, data);

        if (subscription.Id == Guid.Empty)
        {
            subscription = await _subscriptionRepo.CreateAsync(subscription);
        }
        else
        {
            subscription = await _subscriptionRepo.UpdateAsync(subscription);
        }

        // Update user's subscription tier
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.SubscriptionId = subscription.Id;
            user.SubscriptionTier = subscription.Tier;
            await _userManager.UpdateAsync(user);
        }

        return subscription;
    }

    public async Task HandleSubscriptionCancelledAsync(string paddleSubscriptionId, DateTime? cancelledAt)
    {
        var subscription = await _subscriptionRepo.GetByPaddleSubscriptionIdAsync(paddleSubscriptionId);
        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = cancelledAt ?? DateTime.UtcNow;
            await _subscriptionRepo.UpdateAsync(subscription);
        }
    }

    public async Task HandleSubscriptionPausedAsync(string paddleSubscriptionId, DateTime pausedAt)
    {
        var subscription = await _subscriptionRepo.GetByPaddleSubscriptionIdAsync(paddleSubscriptionId);
        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Paused;
            subscription.PausedAt = pausedAt;
            await _subscriptionRepo.UpdateAsync(subscription);
        }
    }

    public async Task HandleSubscriptionResumedAsync(string paddleSubscriptionId)
    {
        var subscription = await _subscriptionRepo.GetByPaddleSubscriptionIdAsync(paddleSubscriptionId);
        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.PausedAt = null;
            await _subscriptionRepo.UpdateAsync(subscription);
        }
    }

    public async Task<Transaction> RecordTransactionAsync(string userId, PaddleEventData data)
    {
        // Check if transaction already exists
        if (await _transactionRepo.ExistsAsync(data.Id))
        {
            return (await _transactionRepo.GetByPaddleTransactionIdAsync(data.Id))!;
        }

        var subscription = await _subscriptionRepo.GetByUserIdAsync(userId);
        
        var transaction = new Transaction
        {
            UserId = userId,
            SubscriptionId = subscription?.Id,
            PaddleTransactionId = data.Id,
            PaddleSubscriptionId = data.SubscriptionId,
            Amount = decimal.TryParse(data.Total, out var amount) ? amount : 0,
            Currency = data.CurrencyCode ?? "USD",
            Status = MapTransactionStatus(data.Status),
            Type = string.IsNullOrEmpty(data.SubscriptionId) 
                ? TransactionType.OneTime 
                : TransactionType.Subscription,
            Details = $"Paddle transaction {data.Id}"
        };

        return await _transactionRepo.CreateAsync(transaction);
    }

    private void UpdateSubscriptionFromPaddleData(Subscription subscription, PaddleEventData data)
    {
        subscription.PaddleSubscriptionId = data.Id;
        subscription.PaddleCustomerId = data.CustomerId;
        subscription.Status = MapSubscriptionStatus(data.Status);
        subscription.CurrentPeriodStart = data.CurrentBillingPeriod_StartsAt;
        subscription.CurrentPeriodEnd = data.CurrentBillingPeriod_EndsAt;
        subscription.CancelledAt = data.CanceledAt;
        subscription.PausedAt = data.PausedAt;
        
        // Determine tier from price ID
        if (data.Items?.Any() == true)
        {
            var priceId = data.Items.First().Price?.Id;
            subscription.PaddlePriceId = priceId;
            subscription.Tier = DetermineTierFromPriceId(priceId);
            subscription.PlanName = subscription.Tier.ToString();
        }
    }

    private SubscriptionTier DetermineTierFromPriceId(string? priceId)
    {
        if (string.IsNullOrEmpty(priceId)) return SubscriptionTier.Starter;

        if (_paddleOptions.PriceIds.TryGetValue("Starter_Monthly", out var starterMonthly) && priceId == starterMonthly)
            return SubscriptionTier.Starter;
        if (_paddleOptions.PriceIds.TryGetValue("Starter_Yearly", out var starterYearly) && priceId == starterYearly)
            return SubscriptionTier.Starter;
        if (_paddleOptions.PriceIds.TryGetValue("Pro_Monthly", out var proMonthly) && priceId == proMonthly)
            return SubscriptionTier.Pro;
        if (_paddleOptions.PriceIds.TryGetValue("Pro_Yearly", out var proYearly) && priceId == proYearly)
            return SubscriptionTier.Pro;
        if (_paddleOptions.PriceIds.TryGetValue("Enterprise_Monthly", out var entMonthly) && priceId == entMonthly)
            return SubscriptionTier.Enterprise;
        if (_paddleOptions.PriceIds.TryGetValue("Enterprise_Yearly", out var entYearly) && priceId == entYearly)
            return SubscriptionTier.Enterprise;

        return SubscriptionTier.Starter;
    }

    private SubscriptionStatus MapSubscriptionStatus(string status)
    {
        return status.ToLower() switch
        {
            "active" => SubscriptionStatus.Active,
            "canceled" => SubscriptionStatus.Cancelled,
            "cancelled" => SubscriptionStatus.Cancelled,
            "past_due" => SubscriptionStatus.PastDue,
            "paused" => SubscriptionStatus.Paused,
            "expired" => SubscriptionStatus.Expired,
            "trialing" => SubscriptionStatus.Trialing,
            _ => SubscriptionStatus.Active
        };
    }

    private TransactionStatus MapTransactionStatus(string status)
    {
        return status.ToLower() switch
        {
            "completed" => TransactionStatus.Completed,
            "paid" => TransactionStatus.Completed,
            "failed" => TransactionStatus.Failed,
            "refunded" => TransactionStatus.Refunded,
            "pending" => TransactionStatus.Pending,
            _ => TransactionStatus.Pending
        };
    }

    private UsageLimits GetLimitsForTier(SubscriptionTier tier)
    {
        var section = _configuration.GetSection($"SubscriptionLimits:{tier}");
        
        return new UsageLimits
        {
            SubmissionsPerMonth = section.GetValue<int>("SubmissionsPerMonth", tier == SubscriptionTier.Starter ? 20 : -1),
            StorageGB = section.GetValue<int>("StorageGB", tier == SubscriptionTier.Starter ? 5 : 25),
            ClientsMax = section.GetValue<int>("ClientsMax", tier == SubscriptionTier.Starter ? 10 : -1),
            AiFeatures = section.GetValue<bool>("AiFeatures", tier != SubscriptionTier.Starter),
            PrioritySupport = section.GetValue<bool>("PrioritySupport", tier == SubscriptionTier.Enterprise)
        };
    }
}
