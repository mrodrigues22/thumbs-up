using System.ComponentModel.DataAnnotations;

namespace ThumbsUpApi.Configuration;

public sealed class AiOptions
{
    [Required(ErrorMessage = "OpenAI API key is required")]
    public string ApiKey { get; set; } = string.Empty;
    
    public string? BaseUrl { get; set; }
    
    [Required(ErrorMessage = "Text model is required")]
    public string TextModel { get; set; } = "gpt-4o-mini";
    
    [Required(ErrorMessage = "Vision model is required")]
    public string VisionModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Toggle to control whether the application uses the /v1/responses endpoint (recommended) or falls back to chat/completions.
    /// </summary>
    public bool UseResponsesApi { get; set; } = true;
}
