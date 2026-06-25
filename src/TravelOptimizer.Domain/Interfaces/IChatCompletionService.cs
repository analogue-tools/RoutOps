namespace TravelOptimizer.Domain.Interfaces;

/// <summary>
/// JSON-mode chat completion, mirroring the repo's existing OpenAIService / AnthropicClient pattern
/// (spec §9). The system prompt fixes the role; the user message packs the data; the model returns
/// strict JSON which the caller deserialises against its own schema.
/// </summary>
public interface IChatCompletionService
{
    Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}
