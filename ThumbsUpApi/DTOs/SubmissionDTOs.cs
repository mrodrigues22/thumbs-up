using System.ComponentModel.DataAnnotations;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.DTOs;

public class CreateSubmissionRequest
{
    // Option 1: Use existing client
    public Guid? ClientId { get; set; }
    
    // Option 2: Create new client or quick email entry
    [EmailAddress]
    public string? ClientEmail { get; set; }
    
    public string? ClientName { get; set; }
    
    public string? Message { get; set; }
    
    public string? Captions { get; set; }
    
    [Required]
    public List<IFormFile> Files { get; set; } = new();
}

public class SubmissionResponse
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? AccessPassword { get; set; }
    public string? Message { get; set; }
    public string? Captions { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<MediaFileResponse> MediaFiles { get; set; } = new();
    public ReviewResponse? Review { get; set; }
    public ContentFeatureResponse? ContentFeature { get; set; }
}

public class MediaFileResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public MediaFileType FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class ValidateAccessRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    [Required]
    public string AccessPassword { get; set; } = string.Empty;
}

public class SubmitReviewRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    [Required]
    public string AccessPassword { get; set; } = string.Empty;
    
    [Required]
    public ReviewStatus Status { get; set; }
    
    public string? Comment { get; set; }
}

public class ReviewResponse
{
    public Guid Id { get; set; }
    public ReviewStatus Status { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewedAt { get; set; }
}

public class ContentFeatureResponse
{
    public string? OcrText { get; set; }
    public List<string> Tags { get; set; } = new();
    public ThemeInsightsResponse? ThemeInsights { get; set; }
    public DateTime? ExtractedAt { get; set; }
    public DateTime? LastAnalyzedAt { get; set; }
    public ContentFeatureStatus? AnalysisStatus { get; set; }
    public string? FailureReason { get; set; }
}

public class ThemeInsightsResponse
{
    public List<string> Subjects { get; set; } = new();
    public List<string> Vibes { get; set; } = new();
    public List<string> NotableElements { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
}
