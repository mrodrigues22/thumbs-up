using System.Net.Http.Json;
using System.Text;

namespace ThumbsUpApi.Services;

public class LlamaTextService : ITextGenerationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlamaTextService> _logger;

    public LlamaTextService(IConfiguration configuration, ILogger<LlamaTextService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var baseUrl = _configuration["Ai:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = _configuration["Ai:Ollama:TextModel"] ?? "llama3"; // or phi3.5
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var fullPrompt = $"<|system|>\n{systemPrompt}\n<|user|>\n{userPrompt}";
            var request = new { model, prompt = fullPrompt, stream = false };
            var response = await client.PostAsJsonAsync("/api/generate", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Text generation failed {StatusCode}", response.StatusCode);
                return string.Empty;
            }
            var payload = await response.Content.ReadFromJsonAsync<OllamaGenResponse>(cancellationToken: ct);
            return payload?.Response ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text generation exception");
            return string.Empty;
        }
    }

    private record OllamaGenResponse(string Response);
}
