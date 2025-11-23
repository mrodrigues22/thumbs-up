using System.Text.Json.Serialization;

namespace ThumbsUpApi.Services;

public class OpenAiOcrService : IImageOcrService
{
    private readonly AiOptions _options;
    private readonly IOpenAiClient _client;
    private readonly ILogger<OpenAiOcrService> _logger;

    public OpenAiOcrService(Microsoft.Extensions.Options.IOptions<AiOptions> options, IOpenAiClient client, ILogger<OpenAiOcrService> logger)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
    }

    public async Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default)
    {
        var model = _options.VisionModel ?? _options.TextModel ?? "gpt-5-mini";

        try
        {
            var bytes = await File.ReadAllBytesAsync(physicalPath, ct);
            var base64 = Convert.ToBase64String(bytes);
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

            var payload = await _client.PostAsync<ChatCompletionsVisionRequest, VisionResponse>("chat/completions", request, ct);
            if (payload == null)
            {
                return null;
            }
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