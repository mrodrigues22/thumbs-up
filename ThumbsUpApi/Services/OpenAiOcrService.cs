using ThumbsUpApi.Configuration;
using ThumbsUpApi.DTOs;

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

    public async Task<string?> ExtractTextAsync(string base64Image, CancellationToken ct = default)
    {
        var model = _options.VisionModel ?? _options.TextModel ?? "gpt-5-mini";

        try
        {
            var prompt = "You are an OCR engine. Return ONLY all visible text from the image in reading order. No commentary.";
            
            var request = new OpenAiChatRequest
            {
                Model = model,
                Messages = new[]
                {
                    new OpenAiMessage
                    {
                        Role = "user",
                        Content = new OpenAiVisionContent[]
                        {
                            new() { Type = "text", Text = prompt },
                            new() { Type = "image_url", ImageUrl = new OpenAiImageUrl { Url = $"data:image/png;base64,{base64Image}" } }
                        }
                    }
                }
            };

            var payload = await _client.PostAsync<OpenAiChatRequest, OpenAiChatResponse>("chat/completions", request, ct);
            if (payload == null)
            {
                return null;
            }
            
            var content = payload.Choices?.FirstOrDefault()?.Message?.GetContentString();
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI OCR exception");
            return null;
        }
    }
}
