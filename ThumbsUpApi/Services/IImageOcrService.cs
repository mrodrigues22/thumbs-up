namespace ThumbsUpApi.Services;

public interface IImageOcrService
{
    /// <summary>
    /// Performs OCR over image bytes or physical path and returns extracted text.
    /// </summary>
    Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default);
}