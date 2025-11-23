using System.Text.Json.Serialization;

namespace ThumbsUpApi.DTOs;

public sealed class OpenAiResponseRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("input")]
    public OpenAiResponseMessage[] Input { get; set; } = Array.Empty<OpenAiResponseMessage>();
    
    [JsonPropertyName("response_format")]
    public object? ResponseFormat { get; set; }
} 

public sealed class OpenAiResponseMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";
    
    [JsonPropertyName("content")]
    public OpenAiResponseContent[] Content { get; set; } = Array.Empty<OpenAiResponseContent>();
}

public sealed class OpenAiResponseContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("image_file")]
    public OpenAiResponseImageFile? ImageFile { get; set; }
}

public sealed class OpenAiResponseImageFile
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;
}

public sealed class OpenAiResponsePayload
{
    [JsonPropertyName("output")]
    public List<OpenAiResponseOutput>? Output { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    public string? GetFirstTextOutput()
    {
        if (Output == null)
        {
            return null;
        }
        
        foreach (var block in Output)
        {
            if (block?.Content == null)
            {
                continue;
            }
            foreach (var content in block.Content)
            {
                if (string.Equals(content?.Type, "output_text", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(content?.Text))
                {
                    return content!.Text!.Trim();
                }
            }
        }
        
        return null;
    }
}

public sealed class OpenAiResponseOutput
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("role")]
    public string? Role { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("content")]
    public List<OpenAiResponseOutputContent>? Content { get; set; }
}

public sealed class OpenAiResponseOutputContent
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public sealed class OpenAiFileUploadResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("filename")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;
}
