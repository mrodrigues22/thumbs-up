using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

public class Transaction
{
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public Guid? SubscriptionId { get; set; }
    
    [Required]
    public string PaddleTransactionId { get; set; } = string.Empty;
    
    public string? PaddleSubscriptionId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    
    [Required]
    public TransactionType Type { get; set; } = TransactionType.Subscription;
    
    public string? Details { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Subscription? Subscription { get; set; }
}

public enum TransactionStatus
{
    Completed = 0,
    Failed = 1,
    Refunded = 2,
    Pending = 3,
    Billed = 4,
    Canceled = 5,
    Ready = 6
}

public enum TransactionType
{
    Subscription = 0,
    OneTime = 1,
    Refund = 2,
    Credit = 3
}
