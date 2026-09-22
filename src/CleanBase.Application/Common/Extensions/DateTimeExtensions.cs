namespace CleanBase.Application.Common.Extensions;

public static class DateTimeExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this DateTime date) =>
        new DateTimeOffset(date, TimeSpan.Zero);
}