using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ThumbsUpApi.Configuration;
using ThumbsUpApi.DTOs;

namespace ThumbsUpApi.Services;

public interface IOpenAiClient
{
    Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default);
    Task<TResponse?> PostResponsesAsync<TRequest, TResponse>(TRequest request, CancellationToken ct = default);
    Task<OpenAiFileUploadResponse?> UploadFileAsync(Stream fileStream, string fileName, string purpose, CancellationToken ct = default);
    Task DeleteFileAsync(string fileId, CancellationToken ct = default);
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
        => await SendJsonAsync<TRequest, TResponse>(path, request, ct);

    public async Task<TResponse?> PostResponsesAsync<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
        => await SendJsonAsync<TRequest, TResponse>("responses", request, ct);

    private async Task<TResponse?> SendJsonAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var client = CreateClient(apiKey);

        using var response = await client.PostAsJsonAsync(path, request, cancellationToken: ct);
        return await ParseResponse<TResponse>(response, path, ct);
    }

    public async Task<OpenAiFileUploadResponse?> UploadFileAsync(Stream fileStream, string fileName, string purpose, CancellationToken ct = default)
    {
        var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var client = CreateClient(apiKey);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(new StringContent(purpose), "purpose");
        content.Add(streamContent, "file", fileName);

        using var response = await client.PostAsync("files", content, ct);
        return await ParseResponse<OpenAiFileUploadResponse>(response, "files", ct);
    }

    public async Task DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var client = CreateClient(apiKey);
        using var response = await client.DeleteAsync($"files/{fileId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("OpenAI file delete failed for {FileId} with {StatusCode}: {Body}", fileId, response.StatusCode, Truncate(body));
        }
    }

    private HttpClient CreateClient(string apiKey)
    {
        var baseUrl = _options.BaseUrl ?? "https://api.openai.com/v1/";
        var client = _httpClientFactory.CreateClient("OpenAiClient");
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }

    private async Task<TResponse?> ParseResponse<TResponse>(HttpResponseMessage response, string path, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("OpenAI request to {Path} failed with {StatusCode}: {Body}", path, response.StatusCode, Truncate(body));
            throw new HttpRequestException($"OpenAI API request failed with status {response.StatusCode}: {Truncate(body, 200)}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    }

    private static string Truncate(string value, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value.Substring(0, maxLength) + "...";
    }
}
