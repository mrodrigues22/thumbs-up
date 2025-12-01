using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Models;

public class ContentFeature
{
    public Guid Id { get; set; }

    [Required]
    public Guid SubmissionId { get; set; }

    public string? OcrText { get; set; }

    // Stored as JSON array of strings for flexibility (e.g., ["minimal","warm","outdoor"]).
    public string? ThemeTagsJson { get; set; }

    public DateTime? ExtractedAt { get; set; }

    public DateTime? LastAnalyzedAt { get; set; }

    public ContentFeatureStatus AnalysisStatus { get; set; } = ContentFeatureStatus.Pending;

    public string? FailureReason { get; set; }

    // Navigation
    public Submission Submission { get; set; } = null!;
}
