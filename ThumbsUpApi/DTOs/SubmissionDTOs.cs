using System.ComponentModel.DataAnnotations;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.DTOs;

public class CreateSubmissionRequest
{
    [Required]
    [EmailAddress]
    public string ClientEmail { get; set; } = string.Empty;
    
    [Required]
    [MinLength(4)]
    public string AccessPassword { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    [Required]
    public List<IFormFile> Files { get; set; } = new();
}

public class SubmissionResponse
{
    public Guid Id { get; set; }
    public string ClientEmail { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? Message { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<MediaFileResponse> MediaFiles { get; set; } = new();
    public ReviewResponse? Review { get; set; }
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
