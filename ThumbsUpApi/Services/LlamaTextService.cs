namespace ThumbsUpApi.Services;

// Deprecated legacy wrapper retained only to avoid build breaks where referenced.
// Delegates to OpenAiTextService; remove references to this type in future refactors.
public class LlamaTextService : ITextGenerationService
{
    private readonly OpenAiTextService _inner;
    public LlamaTextService(OpenAiTextService inner) => _inner = inner;
    public Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => _inner.GenerateAsync(systemPrompt, userPrompt, ct);
}
