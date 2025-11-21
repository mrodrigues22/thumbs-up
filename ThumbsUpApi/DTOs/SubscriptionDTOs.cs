namespace ThumbsUpApi.DTOs;

// Request DTOs
public class CreateCheckoutRequest
{
    public required string PriceId { get; set; }
    public string? SuccessUrl { get; set; }
}

public class UpdateSubscriptionRequest
{
    public required string NewPriceId { get; set; }
}

public class CancelSubscriptionRequest
{
    public bool Immediately { get; set; } = false;
}

// Response DTOs
public class CheckoutResponse
{
    public required string CheckoutUrl { get; set; }
    public required string TransactionId { get; set; }
}

public class SubscriptionResponse
{
    public string Status { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string? PaddleSubscriptionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public UsageStatsResponse Usage { get; set; } = new();
}

public class UsageStatsResponse
{
    public int SubmissionsUsed { get; set; }
    public int SubmissionsLimit { get; set; }
    public long StorageUsedBytes { get; set; }
    public long StorageLimitBytes { get; set; }
    public string StorageUsedFormatted { get; set; } = string.Empty;
    public string StorageLimitFormatted { get; set; } = string.Empty;
    public int SubmissionsPercentage { get; set; }
    public int StoragePercentage { get; set; }
}

public class SubscriptionPlanResponse
{
    public required string Tier { get; set; }
    public required string PriceId { get; set; }
    public required decimal Price { get; set; }
    public required string Currency { get; set; }
    public required string Interval { get; set; }
    public int SubmissionsLimit { get; set; }
    public int ClientsLimit { get; set; }
    public long StorageLimitBytes { get; set; }
    public bool HasAiFeatures { get; set; }
    public bool HasCustomBranding { get; set; }
}

// Webhook DTOs
public class PaddleWebhookEvent
{
    public required string EventType { get; set; }
    public required PaddleEventData Data { get; set; }
}

public class PaddleEventData
{
    public string? SubscriptionId { get; set; }
    public string? CustomerId { get; set; }
    public string? Status { get; set; }
    public string? UserId { get; set; }
    public DateTime? CurrentBillingPeriodStartsAt { get; set; }
    public DateTime? CurrentBillingPeriodEndsAt { get; set; }
    public PaddlePrice? Price { get; set; }
}

public class PaddlePrice
{
    public string? Id { get; set; }
    public string? ProductId { get; set; }
}
