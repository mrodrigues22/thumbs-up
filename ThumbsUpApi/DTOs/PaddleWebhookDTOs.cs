namespace ThumbsUpApi.DTOs;

// Paddle Webhook Event DTOs
public class PaddleWebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public PaddleEventData Data { get; set; } = new();
}

public class PaddleEventData
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public PaddleCustomer? Customer { get; set; }
    public List<PaddleItem>? Items { get; set; }
    public PaddleBillingDetails? BillingDetails { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Total { get; set; }
    public DateTime? CurrentBillingPeriod_StartsAt { get; set; }
    public DateTime? CurrentBillingPeriod_EndsAt { get; set; }
    public DateTime? ScheduledChange_Action { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
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
