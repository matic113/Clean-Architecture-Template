using Microsoft.AspNetCore.Diagnostics;

namespace CleanBase.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var traceId = SentrySdk.CaptureException(exception).ToString();

        var message = env.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred. Please try again later.";

        var errorResponse = ApiErrorResponse.FromException(message, httpContext.Request.Path, traceId);

        httpContext.Response.StatusCode = errorResponse.Status;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }
}
