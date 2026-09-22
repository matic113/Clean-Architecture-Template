namespace CleanBase.Application.Common.Extensions;

public static class StringExtensions
{
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
        {
            return str;
        }

        // Optimized for .NET Core:
        // This avoids creating multiple string instances in memory
        return string.Create(
            str.Length,
            str,
            (span, state) =>
            {
                state.CopyTo(span);
                span[0] = char.ToLowerInvariant(span[0]);
            }
        );
    }
}