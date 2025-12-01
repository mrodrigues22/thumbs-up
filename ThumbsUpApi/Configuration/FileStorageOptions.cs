using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Configuration;

public sealed class FileStorageOptions
{
    [Required(ErrorMessage = "Storage provider is required")]
    public string Provider { get; set; } = "Local";
    
    [Required(ErrorMessage = "Local storage path is required")]
    public string LocalPath { get; set; } = "wwwroot/uploads";
    
    [Range(1, 1000, ErrorMessage = "MaxFileSizeMB must be between 1 and 1000")]
    public int MaxFileSizeMB { get; set; } = 100;
}
