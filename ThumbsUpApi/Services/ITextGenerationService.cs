namespace ThumbsUpApi.Services;

public interface ITextGenerationService
{
    /// <summary>
    /// Generates text given a system + user prompt. Implementation may call local LLM (Ollama etc.).
    /// </summary>
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}