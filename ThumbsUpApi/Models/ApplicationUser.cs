using Microsoft.AspNetCore.Identity;

namespace ThumbsUpApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Subscription fields
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.None;
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.None;
    public string? PaddleSubscriptionId { get; set; }
    public string? PaddleCustomerId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime? SubscriptionCancelledAt { get; set; }
    public int SubmissionsUsedThisMonth { get; set; } = 0;
    public long StorageUsedBytes { get; set; } = 0;
    public DateTime? UsageResetDate { get; set; }
    
    // Navigation property
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
