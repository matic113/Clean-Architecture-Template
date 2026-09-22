using CleanBase.Application.Utilities.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace CleanBase.Infrastructure.Utilities.AI.OpenRouter;

public class OpenRouterCompletionService(
    OpenRouterClient client,
    IOptions<OpenRouterOptions> options,
    ILogger<OpenRouterCompletionService> logger
) : IAICompletionService
{
    private readonly OpenRouterOptions _options = options.Value;

    public async Task<ErrorOr<string>> CompleteAsync(
        string prompt,
        string? systemPrompt = null,
        string? model = null,
        float? temperature = null,
        CancellationToken ct = default
    )
    {
        var response = await CreateCompletionAsync(new AICompletionRequest(
            Prompt: prompt,
            SystemPrompt: systemPrompt,
            Model: model,
            Temperature: temperature
        ), ct);

        if (response.IsError)
            return response.Errors;

        return response.Value.Content;
    }

    public async Task<ErrorOr<AICompletionResponse>> CreateCompletionAsync(
        AICompletionRequest request,
        CancellationToken ct = default
    )
    {
        try
        {
            var messages = new List<Message>();

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new Message
                {
                    Role = "system",
                    Content = request.SystemPrompt
                });
            }

            if (request.Messages is { Count: > 0 })
            {
                foreach (var msg in request.Messages)
                {
                    messages.Add(new Message
                    {
                        Role = msg.Role,
                        Content = msg.Content
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.Prompt))
            {
                messages.Add(new Message
                {
                    Role = "user",
                    Content = request.Prompt
                });
            }

            if (messages.Count == 0)
            {
                return Error.Validation("AI.EmptyPrompt", "No prompt or messages provided for completion.");
            }

            var chosenModel = !string.IsNullOrWhiteSpace(request.Model)
                ? request.Model
                : _options.DefaultModelId;

            var chosenTemp = request.Temperature ?? _options.Temperature;

            var chatRequest = new ChatCompletionRequest
            {
                Model = chosenModel,
                Messages = messages,
                Temperature = chosenTemp,
                MaxTokens = request.MaxTokens,
                Reasoning = (!string.IsNullOrWhiteSpace(request.ReasoningEffort) || request.ReasoningMaxTokens.HasValue)
                    ? new ReasoningConfig
                    {
                        Effort = request.ReasoningEffort,
                        MaxTokens = request.ReasoningMaxTokens,
                        Enabled = true
                    }
                    : null,
            };

            var result = await client.CreateChatCompletionAsync(chatRequest, ct);

            var firstChoice = result.Choices?.FirstOrDefault();
            var content = firstChoice?.Message?.Content?.ToString() ?? string.Empty;

            return new AICompletionResponse(
                Content: content,
                Model: result.Model ?? chosenModel,
                PromptTokens: result.Usage?.PromptTokens,
                CompletionTokens: result.Usage?.CompletionTokens,
                TotalTokens: result.Usage?.TotalTokens
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenRouter chat completion failed");
            return Error.Failure("AI.CompletionFailed", ex.Message);
        }
    }
}
