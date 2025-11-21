using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;

namespace ThumbsUpApi.Services;

public class PaddleService : IPaddleService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaddleService> _logger;
    private readonly string _apiKey;
    private readonly string _webhookSecret;
    private readonly string _baseUrl;

    public PaddleService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<PaddleService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _apiKey = _configuration["Paddle:ApiKey"] ?? throw new InvalidOperationException("Paddle API Key not configured");
        _webhookSecret = _configuration["Paddle:WebhookSecret"] ?? throw new InvalidOperationException("Paddle Webhook Secret not configured");
        
        var environment = _configuration["Paddle:Environment"] ?? "sandbox";
        _baseUrl = environment.ToLower() == "production" 
            ? "https://api.paddle.com" 
            : "https://sandbox-api.paddle.com";
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<CheckoutResponse> CreateCheckoutSessionAsync(string priceId, string userId, string? successUrl = null)
    {
        try
        {
            var payload = new
            {
                items = new[]
                {
                    new { price_id = priceId, quantity = 1 }
                },
                customer = new
                {
                    id = userId
                },
                custom_data = new
                {
                    user_id = userId
                },
                success_url = successUrl ?? _configuration["Paddle:DefaultSuccessUrl"]
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload), 
                Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.PostAsync("/transactions", content);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseData);

            return new CheckoutResponse
            {
                CheckoutUrl = result.GetProperty("data").GetProperty("checkout").GetProperty("url").GetString() ?? "",
                TransactionId = result.GetProperty("data").GetProperty("id").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paddle checkout session");
            throw;
        }
    }

    public async Task<string> GetSubscriptionStatusAsync(string subscriptionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/subscriptions/{subscriptionId}");
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseData);

            return result.GetProperty("data").GetProperty("status").GetString() ?? "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription status from Paddle");
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, bool immediately = false)
    {
        try
        {
            var payload = new
            {
                effective_from = immediately ? "immediately" : "next_billing_period"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload), 
                Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.PostAsync($"/subscriptions/{subscriptionId}/cancel", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<string> UpdateSubscriptionAsync(string subscriptionId, string newPriceId)
    {
        try
        {
            var payload = new
            {
                items = new[]
                {
                    new { price_id = newPriceId, quantity = 1 }
                },
                proration_billing_mode = "prorated_immediately"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload), 
                Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.PatchAsync($"/subscriptions/{subscriptionId}", content);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseData);

            return result.GetProperty("data").GetProperty("id").GetString() ?? subscriptionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<string> GetUpdatePaymentMethodUrlAsync(string subscriptionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/subscriptions/{subscriptionId}/update-payment-method-transaction");
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseData);

            return result.GetProperty("data").GetProperty("payment_method_url").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get update payment URL for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public bool ValidateWebhookSignature(string signature, string requestBody)
    {
        try
        {
            // Paddle uses HMAC SHA256 for webhook signature validation
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var computedSignature = Convert.ToBase64String(hash);

            return signature == computedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate webhook signature");
            return false;
        }
    }
}
