namespace ThumbsUpApi.Services;

// Deprecated legacy wrapper now delegates to OpenAiThemeService.
public class MoondreamThemeService : IImageThemeService
{
    private readonly OpenAiThemeService _inner;
    public MoondreamThemeService(OpenAiThemeService inner) => _inner = inner;
    public Task<IReadOnlyList<string>> ExtractThemesAsync(string physicalPath, CancellationToken ct = default)
        => _inner.ExtractThemesAsync(physicalPath, ct);
}
