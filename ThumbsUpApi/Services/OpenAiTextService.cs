using ThumbsUpApi.Configuration;
using ThumbsUpApi.DTOs;

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
        var model = _options.TextModel ?? _options.VisionModel ?? "gpt-4o-mini";
        return _options.UseResponsesApi
            ? await GenerateViaResponsesAsync(model, systemPrompt, userPrompt, ct)
            : await GenerateViaChatAsync(model, systemPrompt, userPrompt, ct);
    }

    private async Task<string> GenerateViaResponsesAsync(string model, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        try
        {
            var request = new OpenAiResponseRequest
            {
                Model = model,
                Input = new[]
                {
                    new OpenAiResponseMessage
                    {
                        Role = "system",
                        Content = new[] { new OpenAiResponseContent { Type = "input_text", Text = systemPrompt } }
                    },
                    new OpenAiResponseMessage
                    {
                        Role = "user",
                        Content = new[] { new OpenAiResponseContent { Type = "input_text", Text = userPrompt } }
                    }
                }
            };

            var payload = await _client.PostResponsesAsync<OpenAiResponseRequest, OpenAiResponsePayload>(request, ct);
            var content = payload?.GetFirstTextOutput();
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI responses call returned empty content. Model: {Model}", model);
            }
            return content?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI text generation exception for model {Model} (responses)", model);
            return string.Empty;
        }
    }

    private async Task<string> GenerateViaChatAsync(string model, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        try
        {
            var request = new OpenAiChatRequest
            {
                Model = model,
                Messages = new[]
                {
                    new OpenAiMessage { Role = "system", Content = systemPrompt },
                    new OpenAiMessage { Role = "user", Content = userPrompt }
                }
            };

            var payload = await _client.PostAsync<OpenAiChatRequest, OpenAiChatResponse>("chat/completions", request, ct);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.GetContentString();
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI returned empty content. Model: {Model}, ChoicesCount: {Count}", model, payload?.Choices?.Length ?? 0);
            }
            return content?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI text generation exception for model {Model} (chat)", model);
            return string.Empty;
        }
    }
}