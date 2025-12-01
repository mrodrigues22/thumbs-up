using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

/// <summary>
/// Tracks processed webhook events to prevent duplicate processing
/// </summary>
public class ProcessedWebhookEvent
{
    [Key]
    [MaxLength(255)]
    public string EventId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime OccurredAt { get; set; }
    
    [MaxLength(50)]
    public string? SubscriptionId { get; set; }
    
    [MaxLength(50)]
    public string? CustomerId { get; set; }
}
