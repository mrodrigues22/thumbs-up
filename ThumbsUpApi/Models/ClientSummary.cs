using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

public class ClientSummary
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    // Consolidated AI-generated summary of preferences, style, recurring feedback themes.
    [Required]
    public string SummaryText { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Client Client { get; set; } = null!;
}
