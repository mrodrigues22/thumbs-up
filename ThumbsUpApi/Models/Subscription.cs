using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

public class Subscription
{
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public string? PaddleSubscriptionId { get; set; }
    
    public string? PaddleCustomerId { get; set; }
    
    [Required]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    
    [Required]
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Starter;
    
    public string? PaddlePriceId { get; set; }
    
    public string? PlanName { get; set; }
    
    public DateTime? CurrentPeriodStart { get; set; }
    
    public DateTime? CurrentPeriodEnd { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public DateTime? PausedAt { get; set; }
    
    public DateTime? TrialEndsAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
