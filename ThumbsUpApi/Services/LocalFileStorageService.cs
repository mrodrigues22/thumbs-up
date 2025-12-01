using ThumbsUpApi.Configuration;

namespace ThumbsUpApi.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileStorageOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _uploadPath;
    
    public LocalFileStorageService(
        IWebHostEnvironment environment, 
        Microsoft.Extensions.Options.IOptions<FileStorageOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        
        // Get upload path from options
        _uploadPath = Path.Combine(_environment.ContentRootPath, _options.LocalPath);
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }
    
    public async Task<string> UploadAsync(IFormFile file, string folder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));
        
        // Create subfolder if specified
        var targetPath = Path.Combine(_uploadPath, folder);
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        
        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var fullPath = Path.Combine(targetPath, uniqueFileName);
        
        // Save file
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        // Return relative path
        return Path.Combine(folder, uniqueFileName).Replace("\\", "/");
    }

    public async Task<string> UploadFromStreamAsync(Stream stream, string fileName, string folder = "uploads")
    {
        if (stream == null || stream.Length == 0)
            throw new ArgumentException("Stream is empty", nameof(stream));
        
        // Create subfolder if specified
        var targetPath = Path.Combine(_uploadPath, folder);
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        
        // Use provided filename or generate unique one
        var uniqueFileName = fileName;
        var fullPath = Path.Combine(targetPath, uniqueFileName);
        
        // Save file from stream
        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }
        
        // Return relative path
        return Path.Combine(folder, uniqueFileName).Replace("\\", "/");
    }
    
    public Task<bool> DeleteAsync(string filePath)
    {
        try
        {
            var fullPath = GetPhysicalPath(filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public string GetFileUrl(string filePath)
    {
        // Build full URL with the API base URL
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/uploads/{filePath.Replace("\\", "/")}";
        }
        
        // Fallback to relative path
        return $"/uploads/{filePath.Replace("\\", "/")}";
    }
    
    public string GetPhysicalPath(string filePath)
    {
        return Path.Combine(_uploadPath, filePath);
    }
}
