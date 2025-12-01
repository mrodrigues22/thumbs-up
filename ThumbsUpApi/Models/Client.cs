using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

public class Client
{
    public Guid Id { get; set; }
    
    [Required]
    public string CreatedById { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? CompanyName { get; set; }
    
    public string? ProfilePictureUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation properties
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
