namespace ThumbsUpApi.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;
    
    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
        
        // Get upload path from configuration or use default
        var configPath = _configuration["FileStorage:LocalPath"] ?? "wwwroot/uploads";
        _uploadPath = Path.Combine(_environment.ContentRootPath, configPath);
        
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
        // For local storage, return the relative path (will be served as static file)
        return $"/uploads/{filePath}";
    }
    
    public string GetPhysicalPath(string filePath)
    {
        return Path.Combine(_uploadPath, filePath);
    }
}
