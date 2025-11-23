using System.Text.Json.Serialization;

namespace ThumbsUpApi.DTOs;

/// <summary>
/// Shared DTOs for OpenAI API requests and responses
/// </summary>
/// 
public sealed class OpenAiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public OpenAiMessage[] Messages { get; set; } = Array.Empty<OpenAiMessage>();
}

public sealed class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Helper to get string content from either string or VisionContent array
    /// </summary>
    public string GetContentString()
    {
        return Content switch
        {
            string str => str,
            OpenAiVisionContent[] contents => string.Join("\n", 
                contents.Where(c => c.Type == "text").Select(c => c.Text ?? string.Empty)),
            _ => string.Empty
        };
    }
}

public sealed class OpenAiVisionContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("image_url")]
    public OpenAiImageUrl? ImageUrl { get; set; }
}

public sealed class OpenAiImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public sealed class OpenAiChatResponse
{
    [JsonPropertyName("choices")]
    public OpenAiChoice[]? Choices { get; set; }
}

public sealed class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}
