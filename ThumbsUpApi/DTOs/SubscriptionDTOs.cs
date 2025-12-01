using ThumbsUpApi.Models;

namespace ThumbsUpApi.DTOs;

// Response DTOs
public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public SubscriptionTier Tier { get; set; }
    public string? PlanName { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool IsActive { get; set; }
    public bool CanUpgrade { get; set; }
    public UsageLimits Limits { get; set; } = new();
    public UsageStats Usage { get; set; } = new();
}

public class UsageLimits
{
    public int SubmissionsPerMonth { get; set; }
    public int StorageGB { get; set; }
    public int ClientsMax { get; set; }
    public bool AiFeatures { get; set; }
    public bool PrioritySupport { get; set; }
}

public class UsageStats
{
    public int SubmissionsUsed { get; set; }
    public int SubmissionsRemaining { get; set; }
    public decimal StorageUsedGB { get; set; }
    public decimal StorageRemainingGB { get; set; }
    public int ClientsCount { get; set; }
    public int ClientsRemaining { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string PaddleTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public TransactionType Type { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public List<string> Features { get; set; } = new();
    public UsageLimits Limits { get; set; } = new();
    public bool IsPopular { get; set; }
}

public class CheckoutSessionResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
}

// Request DTOs
public class CreateCheckoutRequest
{
    public string PriceId { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class UpdateSubscriptionRequest
{
    public string? PriceId { get; set; }
}

public class CancelSubscriptionRequest
{
    public bool Immediately { get; set; } = false;
    public string? Reason { get; set; }
}
