using System.ComponentModel.DataAnnotations;

namespace ApprooveItApi.Models;

public class Review
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid SubmissionId { get; set; }
    
    [Required]
    public ReviewStatus Status { get; set; }
    
    public string? Comment { get; set; }
    
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Submission Submission { get; set; } = null!;
}

public enum ReviewStatus
{
    Approved,
    Rejected
}
