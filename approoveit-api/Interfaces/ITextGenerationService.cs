namespace ApprooveItApi.Services;

public interface ITextGenerationService
{
    /// <summary>
    /// Generates text given a system + user prompt using configured GPT model.
    /// </summary>
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
