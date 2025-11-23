namespace ThumbsUpApi.Services;

public sealed class AiOptions
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? TextModel { get; set; }
    public string? VisionModel { get; set; }
}
