using CleanBase.Application.Utilities.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using OpenRouter.NET;
using Polly;

namespace CleanBase.Infrastructure.Utilities.AI.OpenRouter;

public static class DependencyInjection
{
    private const string HttpClientName = "openrouter";

    public static IServiceCollection AddOpenRouterServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OpenRouterOptions>()
            .Bind(configuration.GetSection(OpenRouterOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(OpenRouterOptions.SectionName).Get<OpenRouterOptions>()!;

        // The OpenRouter SDK throws typed exceptions and has no built-in retry, so we hand it a resilient
        // HttpClient — the same 429/5xx retry policy (honoring Retry-After) used for Cartesia.
        var httpClientBuilder = services.AddHttpClient(
            HttpClientName, client => client.Timeout = TimeSpan.FromMinutes(2));

        httpClientBuilder.AddResilienceHandler("openrouter", builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
            });
        });

        // Strip the SDK's OpenRouter-only `reasoning` field so the client also works against strict
        // OpenAI-compatible providers (e.g. Groq). Added last ⇒ innermost handler, runs before the socket.
        httpClientBuilder.AddHttpMessageHandler(() => new StripReasoningHandler());

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);

            return new OpenRouterClient(new OpenRouterClientOptions
            {
                ApiKey = opts.ApiKey,
                BaseUrl = opts.BaseUrl,
                HttpClient = httpClient,
                SiteUrl = opts.Referer,
                SiteName = opts.Title,
            });
        });

        services.AddScoped<IAICompletionService, OpenRouterCompletionService>();

        return services;
    }
}
