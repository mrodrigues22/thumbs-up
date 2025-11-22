using System.Net.Http.Json;

namespace ThumbsUpApi.Services;

public class MoondreamThemeService : IImageThemeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MoondreamThemeService> _logger;

    // Calls local Ollama server with moondream/llava model using a describe prompt.
    public MoondreamThemeService(IConfiguration configuration, ILogger<MoondreamThemeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ExtractThemesAsync(string physicalPath, CancellationToken ct = default)
    {
        var baseUrl = _configuration["Ai:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = _configuration["Ai:Ollama:ImageThemeModel"] ?? "moondream"; // could be llava
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            using var fs = File.OpenRead(physicalPath);
            var bytes = await File.ReadAllBytesAsync(physicalPath, ct);
            // Minimal Ollama image prompt call (pseudo since real API may differ).
            var request = new
            {
                model,
                prompt = "List 3-8 concise lowercase theme/style keywords for this image (no spaces inside a keyword, hyphens allowed). Return CSV.",
                images = new[] { Convert.ToBase64String(bytes) }
            };
            var response = await client.PostAsJsonAsync("/api/generate", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Theme extraction failed {StatusCode}", response.StatusCode);
                return Array.Empty<string>();
            }
            var payload = await response.Content.ReadFromJsonAsync<OllamaGenResponse>(cancellationToken: ct);
            var text = payload?.Response ?? string.Empty;
            var tags = text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0 && t.Length < 40)
                .Distinct()
                .ToList();
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Theme extraction exception for {Path}", physicalPath);
            return Array.Empty<string>();
        }
    }

    private record OllamaGenResponse(string Response);
}
