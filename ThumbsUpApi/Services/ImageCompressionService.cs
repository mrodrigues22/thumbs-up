using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace ThumbsUpApi.Services;

public class ImageCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageCompressionService> _logger;

    public ImageCompressionService(ILogger<ImageCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> CompressImageAsync(Stream stream, int maxWidth = 800, int maxHeight = 800, int quality = 85)
    {
        try
        {
            // Load the image
            using var image = await Image.LoadAsync(stream);
            
            _logger.LogInformation("Original image size: {Width}x{Height}", image.Width, image.Height);

            // Calculate new dimensions while maintaining aspect ratio
            var (newWidth, newHeight) = CalculateNewDimensions(image.Width, image.Height, maxWidth, maxHeight);

            // Resize if needed
            if (newWidth != image.Width || newHeight != image.Height)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight),
                    Mode = ResizeMode.Max
                }));
                
                _logger.LogInformation("Resized image to: {Width}x{Height}", newWidth, newHeight);
            }

            // Create output stream
            var outputStream = new MemoryStream();

            // Save as WebP with specified quality
            var encoder = new WebpEncoder
            {
                Quality = quality,
                Method = WebpEncodingMethod.BestQuality
            };

            await image.SaveAsync(outputStream, encoder);
            
            outputStream.Position = 0;
            
            _logger.LogInformation("Compressed image to WebP format, size: {Size} bytes", outputStream.Length);
            
            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image");
            throw new InvalidOperationException("Failed to compress image", ex);
        }
    }

    private static (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var ratioX = (double)maxWidth / originalWidth;
        var ratioY = (double)maxHeight / originalHeight;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(originalWidth * ratio);
        var newHeight = (int)(originalHeight * ratio);

        return (newWidth, newHeight);
    }
}
