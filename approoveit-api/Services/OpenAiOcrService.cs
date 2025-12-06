using ApprooveItApi.Configuration;
using ApprooveItApi.DTOs;

namespace ApprooveItApi.Services;

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
        return await ExtractWithResponsesAsync(model, physicalPath, ct);
    }

    private async Task<string?> ExtractWithResponsesAsync(string model, string physicalPath, CancellationToken ct)
    {
        const string prompt = "You are an OCR engine. Return ONLY all visible text from the image in reading order. No commentary.";

        try
        {
            var dataUri = BuildImageDataUri(physicalPath);

            var request = new OpenAiResponseRequest
            {
                Model = model,
                Input = new[]
                {
                    new OpenAiResponseMessage
                    {
                        Role = "user",
                        Content = new[]
                        {
                            new OpenAiResponseContent { Type = "input_text", Text = prompt },
                            new OpenAiResponseContent { Type = "input_image", ImageUrl = dataUri }
                        }
                    }
                }
            };

            var payload = await _client.PostResponsesAsync<OpenAiResponseRequest, OpenAiResponsePayload>(request, ct);
            return payload?.GetFirstTextOutput();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI OCR exception");
            throw;
        }
    }

    private static string BuildImageDataUri(string physicalPath)
    {
        var bytes = File.ReadAllBytes(physicalPath);
        var ext = Path.GetExtension(physicalPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "jpeg",
            ".png" => "png",
            ".gif" => "gif",
            ".webp" => "webp",
            _ => "png"
        };
        var base64 = Convert.ToBase64String(bytes);
        return $"data:image/{ext};base64,{base64}";
    }
}
