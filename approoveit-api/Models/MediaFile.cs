using System.ComponentModel.DataAnnotations;

namespace ApprooveItApi.Models;

public class MediaFile
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid SubmissionId { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    public MediaFileType FileType { get; set; }
    
    [Required]
    public long FileSize { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public int Order { get; set; }
    
    // Navigation property
    public Submission Submission { get; set; } = null!;
}

public enum MediaFileType
{
    Image,
    Video
}
