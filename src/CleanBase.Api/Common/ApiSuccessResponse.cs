using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanBase.Api.Common;

[JsonConverter(typeof(ApiSuccessResponseConverterFactory))]
public sealed class ApiSuccessResponse<T>(int status, T value)
{
    public bool Success => true;
    public int Status => status;
    public T Value => value;
}

public sealed class ApiSuccessResponseConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(ApiSuccessResponse<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(ApiSuccessResponseConverter<>).MakeGenericType(valueType))!;
    }
}

public sealed class ApiSuccessResponseConverter<T> : JsonConverter<ApiSuccessResponse<T>>
{
    public override ApiSuccessResponse<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, ApiSuccessResponse<T> response, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteBoolean("success", response.Success);
        writer.WriteNumber("status", response.Status);

        writer.WritePropertyName("data");
        JsonSerializer.Serialize(writer, response.Value, options);

        writer.WriteEndObject();
    }
}
