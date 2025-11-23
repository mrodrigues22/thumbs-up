using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ThumbsUpApi.Services;

public class OpenAiTextService : ITextGenerationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiTextService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiTextService(IConfiguration configuration, ILogger<OpenAiTextService> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var apiKey = _configuration["Ai:OpenAi:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured.");
            return string.Empty;
        }

        var model = _configuration["Ai:OpenAi:Model"] ?? "gpt-5-mini";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var request = new ChatCompletionsRequest
            {
                Model = model,
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = userPrompt }
                }
            };

            using var response = await client.PostAsJsonAsync("chat/completions", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI text generation failed {StatusCode}", response.StatusCode);
                return string.Empty;
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionsResponse>(cancellationToken: ct);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
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