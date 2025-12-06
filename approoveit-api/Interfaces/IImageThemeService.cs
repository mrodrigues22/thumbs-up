using ApprooveItApi.Models;

namespace ApprooveItApi.Services;

public interface IImageThemeService
{
    /// <summary>
    /// Extracts structured creative insights (subjects, vibes, notable elements, colors, keywords) from an image.
    /// </summary>
    Task<ThemeInsights> ExtractThemesAsync(string physicalPath, CancellationToken ct = default);
}
