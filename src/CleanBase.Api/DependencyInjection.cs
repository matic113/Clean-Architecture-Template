using System.Text.Json.Serialization;
using CleanBase.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CleanBase.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOpenApiDocumentation();
        services.ConfigureJsonSerialization();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddCors();

        services.AddInfrastructure(configuration);
        services.AddApplication(configuration);

        services.AddJwtErrorResponses();
        services.AddFastEndpoints();

        return services;
    }

    private static void AddOpenApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(
            "v1",
            options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
            }
        );

    // Serialize enums as string names on responses written via WriteAsJsonAsync (ApiSuccessResponse).
    // Request binding is configured separately on the FastEndpoints serializer (see ConfigureApp).
    private static void ConfigureJsonSerialization(this IServiceCollection services) =>
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );

    // Override JWT 401/403 to return ApiErrorResponse instead of empty bodies.
    private static void AddJwtErrorResponses(this IServiceCollection services) =>
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Events.OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(
                        ApiErrorResponse.Unauthorized(context.Request.Path),
                        context.HttpContext.RequestAborted
                    );
                };

                options.Events.OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(
                        ApiErrorResponse.Forbidden(context.Request.Path),
                        context.HttpContext.RequestAborted
                    );
                };
            }
        );
}
