namespace ThumbsUpApi.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the file path
    /// </summary>
    Task<string> UploadAsync(IFormFile file, string folder = "uploads");
    
    /// <summary>
    /// Uploads a file from a stream to storage and returns the file path
    /// </summary>
    Task<string> UploadFromStreamAsync(Stream stream, string fileName, string folder = "uploads");
    
    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<bool> DeleteAsync(string filePath);
    
    /// <summary>
    /// Gets the full URL to access a file
    /// </summary>
    string GetFileUrl(string filePath);
    
    /// <summary>
    /// Gets the physical path to a file
    /// </summary>
    string GetPhysicalPath(string filePath);
}
