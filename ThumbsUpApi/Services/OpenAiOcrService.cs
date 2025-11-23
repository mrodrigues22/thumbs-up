using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ThumbsUpApi.Services;

public class OpenAiOcrService : IImageOcrService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiOcrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiOcrService(IConfiguration configuration, ILogger<OpenAiOcrService> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default)
    {
        var apiKey = _configuration["Ai:OpenAi:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured for OCR.");
            return null;
        }

        var model = _configuration["Ai:OpenAi:VisionModel"] ?? _configuration["Ai:OpenAi:Model"] ?? "gpt-5-mini";

        try
        {
            var bytes = await File.ReadAllBytesAsync(physicalPath, ct);
            var base64 = Convert.ToBase64String(bytes);
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = "You are an OCR engine. Return ONLY all visible text from the image in reading order. No commentary.";
            var request = new ChatCompletionsVisionRequest
            {
                Model = model,
                Messages = new[]
                {
                    new VisionMessage
                    {
                        Role = "user",
                        Content = new VisionContent[]
                        {
                            new VisionContent { Type = "text", Text = prompt },
                            new VisionContent { Type = "image_url", ImageUrl = new VisionImageUrl { Url = $"data:image/png;base64,{base64}" } }
                        }
                    }
                }
            };

            using var response = await client.PostAsJsonAsync("chat/completions", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI OCR failed {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<VisionResponse>(cancellationToken: ct);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.ContentString();
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI OCR exception for {Path}", physicalPath);
            return null;
        }
    }

    private sealed class ChatCompletionsVisionRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("messages")] public VisionMessage[] Messages { get; set; } = Array.Empty<VisionMessage>();
    }

    private sealed class VisionMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public VisionContent[] Content { get; set; } = Array.Empty<VisionContent>();
        public string ContentString() => string.Join("\n", Content.Where(c => c.Type == "text").Select(c => c.Text));
    }

    private sealed class VisionContent
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("image_url")] public VisionImageUrl? ImageUrl { get; set; }
    }

    private sealed class VisionImageUrl
    {
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    }

    private sealed class VisionResponse
    {
        [JsonPropertyName("choices")] public VisionChoice[]? Choices { get; set; }
    }
    private sealed class VisionChoice
    {
        [JsonPropertyName("message")] public VisionMessage? Message { get; set; }
    }
}