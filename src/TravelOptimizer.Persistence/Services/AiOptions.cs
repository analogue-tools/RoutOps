namespace TravelOptimizer.Persistence.Services;

/// <summary>Bound from configuration ("Travel:OpenAI").</summary>
public class AiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
