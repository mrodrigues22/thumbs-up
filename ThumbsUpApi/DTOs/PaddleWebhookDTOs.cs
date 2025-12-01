using System.Text.Json.Serialization;

namespace ThumbsUpApi.DTOs;

// Paddle Webhook Event DTOs
public class PaddleWebhookEvent
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;
    
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;
    
    [JsonPropertyName("occurred_at")]
    public DateTime OccurredAt { get; set; }
    
    [JsonPropertyName("data")]
    public PaddleEventData Data { get; set; } = new();
}

public class PaddleEventData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }
    
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }
    
    [JsonPropertyName("customer")]
    public PaddleCustomer? Customer { get; set; }
    
    [JsonPropertyName("items")]
    public List<PaddleItem>? Items { get; set; }
    
    [JsonPropertyName("billing_details")]
    public PaddleBillingDetails? BillingDetails { get; set; }
    
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }
    
    [JsonPropertyName("total")]
    public string? Total { get; set; }
    
    [JsonPropertyName("current_billing_period")]
    public PaddleBillingPeriod? CurrentBillingPeriod { get; set; }
    
    [JsonPropertyName("scheduled_change")]
    public PaddleScheduledChange? ScheduledChange { get; set; }
    
    [JsonPropertyName("canceled_at")]
    public DateTime? CanceledAt { get; set; }
    
    [JsonPropertyName("paused_at")]
    public DateTime? PausedAt { get; set; }
    
    [JsonPropertyName("trial_ends_at")]
    public DateTime? TrialEndsAt { get; set; }
}

public class PaddleBillingPeriod
{
    [JsonPropertyName("starts_at")]
    public DateTime? StartsAt { get; set; }
    
    [JsonPropertyName("ends_at")]
    public DateTime? EndsAt { get; set; }
}

public class PaddleScheduledChange
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }
    
    [JsonPropertyName("effective_at")]
    public DateTime? EffectiveAt { get; set; }
}

public class PaddleCustomer
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class PaddleItem
{
    public PaddlePrice? Price { get; set; }
    public int Quantity { get; set; }
}

public class PaddlePrice
{
    public string Id { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PaddleProduct? Product { get; set; }
}

public class PaddleProduct
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class PaddleBillingDetails
{
    public bool EnableCheckout { get; set; }
    public PaddlePaymentTerms? PaymentTerms { get; set; }
}

public class PaddlePaymentTerms
{
    public string? Interval { get; set; }
    public int Frequency { get; set; }
}
