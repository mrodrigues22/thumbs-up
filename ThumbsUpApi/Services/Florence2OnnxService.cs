namespace ThumbsUpApi.Services;

// Deprecated legacy OCR service now delegates to OpenAiOcrService.
public class Florence2OnnxService : IImageOcrService
{
    private readonly OpenAiOcrService _inner;
    public Florence2OnnxService(OpenAiOcrService inner) => _inner = inner;
    public Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default)
        => _inner.ExtractTextAsync(physicalPath, ct);
}
