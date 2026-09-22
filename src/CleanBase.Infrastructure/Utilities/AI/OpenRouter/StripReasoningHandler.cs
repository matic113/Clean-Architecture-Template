using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace CleanBase.Infrastructure.Utilities.AI.OpenRouter;

/// <summary>
/// The OpenRouter.NET SDK serializes a default <c>"reasoning": null</c> into chat completion bodies.
/// While OpenRouter accepts this, strict OpenAI-compatible providers (such as Groq) reject unexpected
/// fields with a 400 Bad Request.
/// 
/// This handler ensures:
/// 1. If targeting OpenRouter with an active reasoning config (e.g. DeepSeek-R1 or any other model that supports reasoning), it is preserved.
/// 2. If <c>reasoning</c> is null (SDK default) or targeting a non-OpenRouter provider, it is stripped.
/// </summary>
public sealed class StripReasoningHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is { } content
            && content.Headers.ContentType?.MediaType == "application/json")
        {
            var body = await content.ReadAsStringAsync(cancellationToken);
            if (body.Contains("\"reasoning\"", StringComparison.Ordinal)
                && JsonNode.Parse(body) is JsonObject obj
                && obj.ContainsKey("reasoning"))
            {
                var reasoningValue = obj["reasoning"];
                var isOpenRouter = request.RequestUri?.Host.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase) == true;

                // Strip if null (empty SDK artifact) or if pointing to a non-OpenRouter strict provider
                if (reasoningValue is null || !isOpenRouter)
                {
                    obj.Remove("reasoning");
                    request.Content = new StringContent(obj.ToJsonString(), Encoding.UTF8);
                    request.Content.Headers.ContentType =
                        content.Headers.ContentType ?? new MediaTypeHeaderValue("application/json");
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
