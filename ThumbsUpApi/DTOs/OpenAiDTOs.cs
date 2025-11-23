using System.Text.Json;
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
            JsonElement element => ExtractFromJsonElement(element),
            OpenAiVisionContent[] contents => string.Join("\n", 
                contents.Where(c => c.Type == "text").Select(c => c.Text ?? string.Empty)),
            _ => string.Empty
        };
    }

    private static string ExtractFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join("\n",
                element.EnumerateArray().Select(ExtractFromJsonArrayItem).Where(s => !string.IsNullOrWhiteSpace(s))),
            JsonValueKind.Object => ExtractTextFromJsonObject(element),
            _ => string.Empty
        };
    }

    private static string ExtractFromJsonArrayItem(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Object => ExtractTextFromJsonObject(element),
            _ => string.Empty
        };
    }

    private static string ExtractTextFromJsonObject(JsonElement element)
    {
        if (element.TryGetProperty("text", out var textElement))
        {
            return textElement.GetString() ?? string.Empty;
        }

        if (element.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "text" && element.TryGetProperty("content", out var nestedContent))
        {
            return nestedContent.GetString() ?? string.Empty;
        }

        return string.Empty;
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
