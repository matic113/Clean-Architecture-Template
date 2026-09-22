using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanBase.Infrastructure.Persistence.Extensions;

public static class JsonbConversionExtensions
{
    /// <summary>
    /// Stores the property as PostgreSQL <c>jsonb</c> via System.Text.Json, with a value
    /// comparer that deep-diffs by serialized content. EF change tracking then issues an
    /// UPDATE for the column only when its value actually changes (not on every save), and
    /// correctly detects mutations of reference types. Works for lists and complex objects.
    /// </summary>
    public static PropertyBuilder<T> HasJsonbConversion<T>(this PropertyBuilder<T> builder)
    {
        var comparer = new ValueComparer<T>(
            (left, right) =>
                JsonSerializer.Serialize(left, (JsonSerializerOptions?)null)
                == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value =>
                value == null
                    ? 0
                    : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
            value =>
                JsonSerializer.Deserialize<T>(
                    JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                    (JsonSerializerOptions?)null
                )!
        );

        builder
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<T>(value, (JsonSerializerOptions?)null)!
            );

        builder.Metadata.SetValueComparer(comparer);

        return builder;
    }
}
