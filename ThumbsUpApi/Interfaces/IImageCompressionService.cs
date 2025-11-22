namespace ThumbsUpApi.Services;

public interface IImageCompressionService
{
    /// <summary>
    /// Compresses an image to WebP format and resizes if needed
    /// </summary>
    /// <param name="stream">Input image stream</param>
    /// <param name="maxWidth">Maximum width (default 800px)</param>
    /// <param name="maxHeight">Maximum height (default 800px)</param>
    /// <param name="quality">WebP quality 1-100 (default 85)</param>
    /// <returns>Compressed image stream</returns>
    Task<Stream> CompressImageAsync(Stream stream, int maxWidth = 800, int maxHeight = 800, int quality = 85);
}
