using System.Text.Json.Serialization;

namespace ThumbsUpApi.Services;

public class OpenAiTextService : ITextGenerationService
{
    private readonly AiOptions _options;
    private readonly IOpenAiClient _client;
    private readonly ILogger<OpenAiTextService> _logger;

    public OpenAiTextService(Microsoft.Extensions.Options.IOptions<AiOptions> options, IOpenAiClient client, ILogger<OpenAiTextService> logger)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var model = _options.TextModel ?? _options.VisionModel ?? "gpt-5-mini";

        try
        {
            var request = new ChatCompletionsRequest
            {
                Model = model,
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = userPrompt }
                }
            };

            var payload = await _client.PostAsync<ChatCompletionsRequest, ChatCompletionsResponse>("chat/completions", request, ct);
            if (payload == null)
            {
                return string.Empty;
            }

            var content = payload.Choices?.FirstOrDefault()?.Message?.Content;
            return content?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI text generation exception");
            return string.Empty;
        }
    }

    private sealed class ChatCompletionsRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("messages")] public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatCompletionsResponse
    {
        [JsonPropertyName("choices")] public ChatChoice[]? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
    }
}