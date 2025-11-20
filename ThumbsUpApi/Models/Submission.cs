using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

public class Submission
{
    public Guid Id { get; set; }
    
    [Required]
    public string CreatedById { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string ClientEmail { get; set; } = string.Empty;
    
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    [Required]
    public string AccessPasswordHash { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    // Navigation properties
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
    public Review? Review { get; set; }
}

public enum SubmissionStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}
