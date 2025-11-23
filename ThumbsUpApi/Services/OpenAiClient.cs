using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ThumbsUpApi.Services;

public interface IOpenAiClient
{
    Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default);
}

public sealed class OpenAiClient : IOpenAiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiClient> _logger;

    public OpenAiClient(IHttpClientFactory httpClientFactory, Microsoft.Extensions.Options.IOptions<AiOptions> options, ILogger<OpenAiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
    {
        var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured.");
            return default;
        }

        var baseUrl = _options.BaseUrl ?? "https://api.openai.com/v1/";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await client.PostAsJsonAsync(path, request, cancellationToken: ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenAI request to {Path} failed with {StatusCode}: {Body}", path, response.StatusCode, Truncate(body));
                return default;
            }

            var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI request to {Path} threw an exception", path);
            return default;
        }
    }

    private static string Truncate(string value, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value.Substring(0, maxLength) + "...";
    }
}
