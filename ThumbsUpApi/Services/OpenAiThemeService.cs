using ThumbsUpApi.Configuration;
using ThumbsUpApi.DTOs;

namespace ThumbsUpApi.Services;

public class OpenAiThemeService : IImageThemeService
{
    private readonly AiOptions _options;
    private readonly IOpenAiClient _client;
    private readonly ILogger<OpenAiThemeService> _logger;

    public OpenAiThemeService(Microsoft.Extensions.Options.IOptions<AiOptions> options, IOpenAiClient client, ILogger<OpenAiThemeService> logger)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ExtractThemesAsync(string physicalPath, CancellationToken ct = default)
    {
        var model = _options.VisionModel ?? _options.TextModel ?? "gpt-5-mini";

        try
        {
            var bytes = await File.ReadAllBytesAsync(physicalPath, ct);
            var base64 = Convert.ToBase64String(bytes);
            var prompt = "List 3-8 concise lowercase one-word or hyphenated style/theme keywords for this image. Return comma-separated list only.";
            
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
                            new() { Type = "image_url", ImageUrl = new OpenAiImageUrl { Url = $"data:image/png;base64,{base64}" } }
                        }
                    }
                }
            };

            var payload = await _client.PostAsync<OpenAiChatRequest, OpenAiChatResponse>("chat/completions", request, ct);
            if (payload == null)
            {
                return Array.Empty<string>();
            }
            
            var text = payload.Choices?.FirstOrDefault()?.Message?.GetContentString() ?? string.Empty;
            var tags = text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0 && t.Length < 40)
                .Distinct()
                .ToList();
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI theme extraction exception for {Path}", physicalPath);
            return Array.Empty<string>();
        }
    }
}