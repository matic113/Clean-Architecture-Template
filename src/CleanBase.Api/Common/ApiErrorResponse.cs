using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.Results;
using CleanBase.Application.Common.Extensions;

namespace CleanBase.Api.Common;

public sealed record ApiErrorItem(string Field, string Message);

[JsonConverter(typeof(ApiErrorResponseConverter))]
public sealed class ApiErrorResponse
{
    public bool Success => false;
    public int Status { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Instance { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IEnumerable<ApiErrorItem> Errors { get; init; } = [];
    public string? TraceId { get; init; }

    public static ApiErrorResponse FromErrors(List<Error> errors, string? instance)
    {
        var firstError = errors.First();

        // Quota/paywall errors are declared as Error.Custom(type: 402) — map them to HTTP 402 so the
        // mobile app can trigger the RevenueCat paywall. The switch below is on the ErrorType enum, which
        // a custom numeric type would fall through to 500, so this guard runs first.
        (int statusCode, string title) = firstError.NumericType == StatusCodes.Status402PaymentRequired
            ? (StatusCodes.Status402PaymentRequired, "Payment Required")
            : firstError.Type switch
            {
                ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
                ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Failed"),
                ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
                ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
                _ => (StatusCodes.Status500InternalServerError, "An error occurred"),
            };

        return new ApiErrorResponse
        {
            Status = statusCode,
            Title = title,
            Instance = instance,
            ErrorCode = firstError.Code,
            Message = firstError.Description,
            Errors = errors.Select(e => new ApiErrorItem(
                Field: e.Type == ErrorType.Validation ? e.Code.ToCamelCase() : "logicalError",
                Message: e.Description)),
        };
    }

    public static ApiErrorResponse FromValidationFailures(
        IEnumerable<ValidationFailure> failures,
        string? instance,
        int statusCode)
    {
        var list = failures.ToList();
        var first = list.FirstOrDefault();

        return new ApiErrorResponse
        {
            Status = statusCode,
            Title = "Validation Failed",
            Instance = instance,
            ErrorCode = first?.PropertyName.ToCamelCase() ?? "ValidationFailed",
            Message = first?.ErrorMessage ?? "One or more validation errors occurred.",
            Errors = list.Select(f => new ApiErrorItem(
                Field: f.PropertyName.ToCamelCase() is { Length: > 0 } n ? n : "logicalError",
                Message: f.ErrorMessage)),
        };
    }

    public static ApiErrorResponse FromException(string message, string? instance, string? traceId = null) =>
        new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Instance = instance,
            ErrorCode = "Server.UnhandledException",
            Message = message,
            Errors = [new ApiErrorItem("exception", message)],
            TraceId = traceId,
        };

    public static ApiErrorResponse Unauthorized(string? instance) =>
        new()
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Instance = instance,
            ErrorCode = "Identity.Unauthorized",
            Message = "Authentication is required to access this resource.",
            Errors = [new ApiErrorItem("logicalError", "Authentication is required to access this resource.")],
        };

    public static ApiErrorResponse Forbidden(string? instance) =>
        new()
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Instance = instance,
            ErrorCode = "Identity.Forbidden",
            Message = "You do not have permission to access this resource.",
            Errors = [new ApiErrorItem("logicalError", "You do not have permission to access this resource.")],
        };
}

public sealed class ApiErrorResponseConverter : JsonConverter<ApiErrorResponse>
{
    public override ApiErrorResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, ApiErrorResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteBoolean("success", value.Success);
        writer.WriteNumber("status", value.Status);
        writer.WriteString("title", value.Title);
        writer.WriteString("instance", value.Instance);
        writer.WriteString("errorCode", value.ErrorCode);
        writer.WriteString("message", value.Message);

        writer.WritePropertyName("errors");
        JsonSerializer.Serialize(writer, value.Errors, options);

        if (value.TraceId is not null)
            writer.WriteString("traceId", value.TraceId);

        writer.WriteEndObject();
    }
}
