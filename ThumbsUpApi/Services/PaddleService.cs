using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ThumbsUpApi.Configuration;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Interfaces;

namespace ThumbsUpApi.Services;

public class PaddleService : IPaddleService
{
    private readonly HttpClient _httpClient;
    private readonly PaddleOptions _options;
    private readonly ILogger<PaddleService> _logger;

    public PaddleService(
        IHttpClientFactory httpClientFactory,
        IOptions<PaddleOptions> options,
        ILogger<PaddleService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _options = options.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> CreateCustomerAsync(string email, string name)
    {
        try
        {
            var payload = new
            {
                email,
                name
            };

            var response = await _httpClient.PostAsJsonAsync("/customers", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaddleCustomerResponse>();
            return result?.Data?.Id ?? throw new Exception("Failed to create customer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Paddle customer for {Email}", email);
            throw;
        }
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string customerId,
        string priceId,
        string? successUrl = null,
        string? cancelUrl = null)
    {
        try
        {
            _logger.LogInformation("Creating checkout session for customer {CustomerId} with price {PriceId}", customerId, priceId);
            
            var payload = new
            {
                customer_id = customerId,
                items = new[]
                {
                    new { price_id = priceId, quantity = 1 }
                },
                custom_data = new { source = "thumbsup_app" },
                success_url = successUrl,
                cancel_url = cancelUrl
            };

            var response = await _httpClient.PostAsJsonAsync("/transactions", payload);
            
            // Read the response content for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paddle API error: Status={StatusCode}, Response={Response}", 
                    response.StatusCode, responseContent);
                throw new Exception($"Paddle API returned {response.StatusCode}: {responseContent}");
            }

            _logger.LogDebug("Paddle API response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<PaddleTransactionResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (result?.Data == null)
            {
                _logger.LogError("Paddle API returned null data. Raw response: {Response}", responseContent);
                throw new Exception("Paddle API returned invalid response");
            }
            
            if (string.IsNullOrEmpty(result.Data.CheckoutUrl))
            {
                _logger.LogError("Paddle API returned empty checkout URL. Raw response: {Response}", responseContent);
                throw new Exception("Paddle API returned empty checkout URL");
            }
            
            _logger.LogInformation("Checkout session created: TransactionId={TransactionId}, CheckoutUrl={CheckoutUrl}", 
                result.Data.Id, result.Data.CheckoutUrl);
            
            return new CheckoutSessionResponse
            {
                CheckoutUrl = result.Data.CheckoutUrl,
                TransactionId = result.Data.Id
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating checkout session for customer {CustomerId}", customerId);
            throw new Exception("Failed to connect to Paddle API. Please check your internet connection and try again.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<PaddleEventData> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/subscriptions/{subscriptionId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaddleSubscriptionResponse>();
            return result?.Data ?? throw new Exception("Subscription not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(string subscriptionId, bool immediately = false)
    {
        try
        {
            var payload = new
            {
                effective_from = immediately ? "immediately" : "next_billing_period"
            };

            var response = await _httpClient.PostAsJsonAsync($"/subscriptions/{subscriptionId}/cancel", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> PauseSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var payload = new
            {
                effective_from = "next_billing_period"
            };

            var response = await _httpClient.PostAsJsonAsync($"/subscriptions/{subscriptionId}/pause", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> ResumeSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var payload = new
            {
                effective_from = "immediately"
            };

            var response = await _httpClient.PostAsJsonAsync($"/subscriptions/{subscriptionId}/resume", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<string> GetCustomerPortalUrlAsync(string customerId)
    {
        try
        {
            var payload = new
            {
                customer_id = customerId
            };

            var response = await _httpClient.PostAsJsonAsync("/customer-portal-sessions", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaddlePortalResponse>();
            return result?.Data?.Url ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer portal session for {CustomerId}", customerId);
            throw;
        }
    }

    public bool VerifyWebhookSignature(string signature, string requestBody)
    {
        try
        {
            var parts = signature.Split(';');
            var ts = parts.FirstOrDefault(p => p.StartsWith("ts="))?.Substring(3);
            var h1 = parts.FirstOrDefault(p => p.StartsWith("h1="))?.Substring(3);

            if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(h1))
                return false;

            var signedPayload = $"{ts}:{requestBody}";
            var secret = Encoding.UTF8.GetBytes(_options.WebhookSecret);
            
            using var hmac = new HMACSHA256(secret);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var calculatedSignature = Convert.ToHexString(hash).ToLower();

            return calculatedSignature == h1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    // Helper response classes
    private class PaddleCustomerResponse
    {
        public PaddleCustomerData? Data { get; set; }
    }

    private class PaddleCustomerData
    {
        public string Id { get; set; } = string.Empty;
    }

    private class PaddleTransactionResponse
    {
        public PaddleTransactionData? Data { get; set; }
    }

    private class PaddleTransactionData
    {
        public string Id { get; set; } = string.Empty;
        
        // Paddle API uses snake_case
        public string? checkout_url { get; set; }
        
        // Also check for the nested checkout object format
        public PaddleCheckout? Checkout { get; set; }
        
        // Computed property for ease of use
        public string CheckoutUrl => checkout_url ?? Checkout?.Url ?? string.Empty;
    }
    
    private class PaddleCheckout
    {
        public string? Url { get; set; }
    }

    private class PaddleSubscriptionResponse
    {
        public PaddleEventData? Data { get; set; }
    }

    private class PaddlePortalResponse
    {
        public PaddlePortalData? Data { get; set; }
    }

    private class PaddlePortalData
    {
        public string Url { get; set; } = string.Empty;
    }
}
