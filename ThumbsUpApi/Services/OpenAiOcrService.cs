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

    public async Task<string?> ExtractTextAsync(string physicalPath, CancellationToken ct = default)
    {
        var model = _options.VisionModel ?? _options.TextModel ?? "gpt-4o-mini";

        try
        {
            var dataUrl = await BuildImageDataUrlAsync(physicalPath, ct);
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
                            new() { Type = "image_url", ImageUrl = new OpenAiImageUrl { Url = dataUrl } }
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

    private static async Task<string> BuildImageDataUrlAsync(string physicalPath, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(physicalPath, ct);
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = GetMimeType(Path.GetExtension(physicalPath));
        return $"data:{mimeType};base64,{base64}";
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".tif" or ".tiff" => "image/tiff",
            ".avif" => "image/avif",
            _ => "image/png"
        };
    }
}
