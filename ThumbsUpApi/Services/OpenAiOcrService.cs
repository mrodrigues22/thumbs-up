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
        return await ExtractWithResponsesAsync(model, physicalPath, ct);
    }

    private async Task<string?> ExtractWithResponsesAsync(string model, string physicalPath, CancellationToken ct)
    {
        const string prompt = "You are an OCR engine. Return ONLY all visible text from the image in reading order. No commentary.";
        string? fileId = null;

        try
        {
            await using (var stream = File.OpenRead(physicalPath))
            {
                var upload = await _client.UploadFileAsync(stream, Path.GetFileName(physicalPath), "vision", ct)
                             ?? throw new InvalidOperationException("OpenAI returned no payload for file upload");
                fileId = upload.Id;
            }

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
                            new OpenAiResponseContent { Type = "input_image", ImageFile = new OpenAiResponseImageFile { FileId = fileId! } }
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
        finally
        {
            await DeleteUploadedFileAsync(fileId);
        }
    }

    private async Task DeleteUploadedFileAsync(string? fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return;
        }

        try
        {
            await _client.DeleteFileAsync(fileId, CancellationToken.None);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(cleanupEx, "Failed to delete temporary OpenAI file {FileId}", fileId);
        }
    }
}
