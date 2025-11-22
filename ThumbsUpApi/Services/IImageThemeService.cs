namespace ThumbsUpApi.Services;

public interface IImageThemeService
{
    /// <summary>
    /// Extracts coarse semantic/theme tags from an image.
    /// </summary>
    Task<IReadOnlyList<string>> ExtractThemesAsync(string physicalPath, CancellationToken ct = default);
}