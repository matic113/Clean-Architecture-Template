using System.ComponentModel.DataAnnotations;

namespace CleanBase.Infrastructure.Utilities.AI.OpenRouter;

/// <summary>
/// OpenRouter integration settings (the chat/LLM provider for story writing). Bound from the
/// <c>OpenRouter</c> configuration section; the API key is supplied via environment/user-secrets.
/// </summary>
public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    /// <summary>Default OpenRouter model slug (e.g. "google/gemini-2.5-flash").</summary>
    public string DefaultModelId { get; set; } = "google/gemini-2.5-flash";

    /// <summary>Default sampling temperature (0.0 = deterministic, 1.0 = creative).</summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>Optional attribution shown on the OpenRouter dashboard (sent as the <c>HTTP-Referer</c> header).</summary>
    public string? Referer { get; set; }

    /// <summary>Optional attribution shown on the OpenRouter dashboard (sent as the <c>X-Title</c> header).</summary>
    public string? Title { get; set; }

    /// <summary>Transport-level retry attempts for transient 429/5xx responses.</summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
