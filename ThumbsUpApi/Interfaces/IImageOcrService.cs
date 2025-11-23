namespace ThumbsUpApi.Services;

public interface IImageOcrService
{
    /// <summary>
    /// Performs OCR over an image file at the provided physical path and returns extracted text.
    /// </summary>
    Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default);
}