namespace CleanBase.Application.Common.Abstractions;

public static class ErrorOrExtensions
{
    public static string FirstErrorMessage<TValue>(this ErrorOr<TValue> result)
    {
        return result.IsError ? result.FirstError.ToFormattedString() : string.Empty;
    }

    public static string ToFormattedString(this Error error)
    {
        return $"{error.Code}: {error.Description}";
    }
}