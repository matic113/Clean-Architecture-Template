namespace CleanBase.Api.Common;

public static class EndpointExtensions
{
    // --- IResponseSender extensions (idiomatic FastEndpoints `await Send.*`) ---

    /// <summary>Sends a 200 OK response with ApiSuccessResponse, or an error response if result has errors.</summary>
    public static Task OkAsync<TResponse>(
        this IResponseSender sender,
        ErrorOr<TResponse> result,
        CancellationToken cancellationToken = default
    ) => sender.ResultAsync(result, cancellationToken);

    /// <summary>Sends a 200 OK response mapped with the given selector, or an error response if result has errors.</summary>
    public static Task OkAsync<TResult, TResponse>(
        this IResponseSender sender,
        ErrorOr<TResult> result,
        Func<TResult, TResponse> mapper,
        CancellationToken cancellationToken = default
    ) => sender.ResultAsync(result, mapper, cancellationToken);

    /// <summary>Sends a 200 OK response with ApiSuccessResponse, or an error response if result has errors.</summary>
    public static Task ResultAsync<TResponse>(
        this IResponseSender sender,
        ErrorOr<TResponse> result,
        CancellationToken cancellationToken = default
    ) => sender.ResultAsync(result, static v => v, cancellationToken);

    /// <summary>Sends a 200 OK response mapped with the given selector, or an error response if result has errors.</summary>
    public static Task ResultAsync<TResult, TResponse>(
        this IResponseSender sender,
        ErrorOr<TResult> result,
        Func<TResult, TResponse> mapper,
        CancellationToken cancellationToken = default
    ) => SendResultCoreAsync(sender.HttpContext.Response, result, mapper, cancellationToken);

    /// <summary>Sends a 200 response with success=true for ErrorOr&lt;Success&gt;, or an error response.</summary>
    public static Task NoContentAsync(
        this IResponseSender sender,
        ErrorOr<Success> result,
        CancellationToken cancellationToken = default
    ) => sender.ResultAsync(result, static _ => new { }, cancellationToken);

    /// <summary>Sends a 200 response with success=true for ErrorOr&lt;Success&gt;, or an error response.</summary>
    public static Task NoContentResultAsync(
        this IResponseSender sender,
        ErrorOr<Success> result,
        CancellationToken cancellationToken = default
    ) => sender.NoContentAsync(result, cancellationToken);

    /// <summary>Sends a formatted error response directly for a list of errors.</summary>
    public static Task ErrorsAsync(
        this IResponseSender sender,
        List<Error> errors,
        CancellationToken cancellationToken = default
    ) => sender.HttpContext.Response.SendErrorResponseAsync(errors, cancellationToken);

    // --- BaseEndpoint extensions (backward compatibility for `this.Send*`) ---

    public static Task SendResultAsync<TResponse>(
        this BaseEndpoint endpoint,
        ErrorOr<TResponse> result,
        CancellationToken cancellationToken = default
    ) => endpoint.SendResultAsync(result, static v => v, cancellationToken);

    public static Task SendResultAsync<TResult, TResponse>(
        this BaseEndpoint endpoint,
        ErrorOr<TResult> result,
        Func<TResult, TResponse> mapper,
        CancellationToken cancellationToken = default
    ) => SendResultCoreAsync(endpoint.HttpContext.Response, result, mapper, cancellationToken);

    public static Task SendNoContentResultAsync(
        this BaseEndpoint endpoint,
        ErrorOr<Success> result,
        CancellationToken cancellationToken = default
    ) => endpoint.SendResultAsync(result, static _ => new { }, cancellationToken);

    public static Task SendErrorsAsync(
        this BaseEndpoint endpoint,
        List<Error> errors,
        CancellationToken cancellationToken = default
    ) => endpoint.HttpContext.Response.SendErrorResponseAsync(errors, cancellationToken);

    // --- Internal HTTP response serialization helpers ---

    private static async Task SendResultCoreAsync<TResult, TResponse>(
        HttpResponse response,
        ErrorOr<TResult> result,
        Func<TResult, TResponse> mapper,
        CancellationToken cancellationToken
    )
    {
        if (result.IsError)
        {
            await response.SendErrorResponseAsync(result.Errors, cancellationToken);
            return;
        }

        await response.SendSuccessResponseAsync(mapper(result.Value), cancellationToken);
    }

    internal static Task SendErrorResponseAsync(
        this HttpResponse httpResponse,
        List<Error> errors,
        CancellationToken cancellationToken = default
    )
    {
        var errorResponse = ApiErrorResponse.FromErrors(errors, httpResponse.HttpContext.Request.Path);
        httpResponse.StatusCode = errorResponse.Status;
        return httpResponse.WriteAsJsonAsync(errorResponse, cancellationToken);
    }

    private static Task SendSuccessResponseAsync<T>(
        this HttpResponse httpResponse,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        httpResponse.StatusCode = StatusCodes.Status200OK;
        return httpResponse.WriteAsJsonAsync(new ApiSuccessResponse<T>(StatusCodes.Status200OK, value), cancellationToken);
    }
}
