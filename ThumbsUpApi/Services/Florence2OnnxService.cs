using System.Net.Http.Json;

namespace ThumbsUpApi.Services;

public class Florence2OnnxService : IImageOcrService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<Florence2OnnxService> _logger;

    // In dev we assume an ONNX runtime server or container exposes an HTTP endpoint for OCR.
    // Fallback: return null (no text) to keep pipeline resilient.
    public Florence2OnnxService(IConfiguration configuration, ILogger<Florence2OnnxService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default)
    {
        var endpoint = _configuration["Ai:Florence2:OcrEndpoint"]; // e.g. http://localhost:8081/ocr
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogWarning("Florence2 OCR endpoint not configured; returning null OCR text.");
            return null;
        }

        try
        {
            using var client = new HttpClient();
            using var fs = File.OpenRead(physicalPath);
            var form = new MultipartFormDataContent
            {
                { new StreamContent(fs), "file", Path.GetFileName(physicalPath) }
            };
            var response = await client.PostAsync(endpoint, form, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Florence2 OCR request failed {StatusCode}", response.StatusCode);
                return null;
            }
            var payload = await response.Content.ReadFromJsonAsync<FlorenceOcrResponse>(cancellationToken: ct);
            return payload?.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Florence2 OCR exception for {Path}", physicalPath);
            return null;
        }
    }

    private record FlorenceOcrResponse(string? Text);
}
