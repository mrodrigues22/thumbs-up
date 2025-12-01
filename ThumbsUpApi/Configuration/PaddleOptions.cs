using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Configuration;

public class PaddleOptions
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    [Required]
    public string WebhookSecret { get; set; } = string.Empty;
    
    [Required]
    public string Environment { get; set; } = "sandbox"; // "sandbox" or "production"
    
    public string ApiBaseUrl => Environment == "production" 
        ? "https://api.paddle.com" 
        : "https://sandbox-api.paddle.com";
    
    public Dictionary<string, string> PriceIds { get; set; } = new();
}
