using Microsoft.AspNetCore.Identity;

namespace ThumbsUpApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Subscription properties
    public string? PaddleCustomerId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Starter;
    
    // Navigation properties
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public Subscription? Subscription { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
