namespace CleanBase.Application.Utilities.AI;

public record ChatMessage(string Role, string Content)
{
    public static ChatMessage User(string content) => new("user", content);
    public static ChatMessage Assistant(string content) => new("assistant", content);
    public static ChatMessage System(string content) => new("system", content);
}

public record AICompletionRequest(
    string? Prompt = null,
    IReadOnlyList<ChatMessage>? Messages = null,
    string? SystemPrompt = null,
    string? Model = null,
    float? Temperature = null,
    int? MaxTokens = null,
    string? ReasoningEffort = null,
    int? ReasoningMaxTokens = null
);

public record AICompletionResponse(
    string Content,
    string Model,
    int? PromptTokens = null,
    int? CompletionTokens = null,
    int? TotalTokens = null,
    string? Reasoning = null
);

public interface IAICompletionService
{
    /// <summary>
    /// Quick completion helper from a single text prompt and optional system instruction.
    /// </summary>
    Task<ErrorOr<string>> CompleteAsync(
        string prompt,
        string? systemPrompt = null,
        string? model = null,
        float? temperature = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Executes a multi-turn chat or parameterized completion request.
    /// </summary>
    Task<ErrorOr<AICompletionResponse>> CreateCompletionAsync(
        AICompletionRequest request,
        CancellationToken ct = default
    );
}
