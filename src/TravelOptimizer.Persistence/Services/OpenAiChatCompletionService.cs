using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelOptimizer.Domain.Interfaces;

namespace TravelOptimizer.Persistence.Services;

/// <summary>
/// JSON-mode chat completion against OpenAI, mirroring the repo's existing OpenAIService pattern
/// (spec §9). Uses response_format=json_object so the model returns strict JSON.
/// </summary>
public class OpenAiChatCompletionService(HttpClient http, IOptions<AiOptions> options) : IChatCompletionService
{
    private readonly AiOptions _opts = options.Value;

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured (Travel:OpenAI:ApiKey).");

        var request = new
        {
            model = _opts.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        using var msg = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(request),
        };
        msg.Headers.Add("Authorization", $"Bearer {_opts.ApiKey}");

        using var resp = await http.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        return doc.RootElement
                   .GetProperty("choices")[0]
                   .GetProperty("message")
                   .GetProperty("content")
                   .GetString()
               ?? throw new InvalidOperationException("OpenAI returned an empty completion.");
    }
}
