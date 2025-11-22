using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ThumbsUpApi.Services;

public class Florence2OnnxService : IImageOcrService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<Florence2OnnxService> _logger;

    public Florence2OnnxService(IConfiguration configuration, ILogger<Florence2OnnxService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default)
    {
        var baseUrl = _configuration["Ai:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = _configuration["Ai:Ollama:OcrModel"] ?? "llava";

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("Ollama base URL not configured; returning null OCR text.");
            return null;
        }

        try
        {
            var imageBytes = await File.ReadAllBytesAsync(physicalPath, ct);
            var imageBase64 = Convert.ToBase64String(imageBytes);

            using var client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            var request = new OllamaGenerateRequest
            {
                Model = model,
                Prompt = "You are an OCR engine. Read ALL visible text in this image and return only the text, line by line, in natural reading order, with no extra commentary.",
                Images = new[] { imageBase64 },
                Stream = false
            };

            using var response = await client.PostAsJsonAsync("/api/generate", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LLaVA OCR request failed {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
            var text = payload?.Response;

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLaVA/Ollama OCR exception for {Path}", physicalPath);
            return null;
        }
    }

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public string[] Images { get; set; } = Array.Empty<string>();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
        }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
