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

}
