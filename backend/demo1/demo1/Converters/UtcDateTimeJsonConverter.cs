using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace demo1.Converters;

public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTime(out var date))
        {
            return date.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc) 
                : date.ToUniversalTime();
        }
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcDate = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        writer.WriteStringValue(utcDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture));
    }
}

public class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.TryGetDateTime(out var date))
            {
                return date.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(date, DateTimeKind.Utc) 
                    : date.ToUniversalTime();
            }
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        var utcDate = value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();

        writer.WriteStringValue(utcDate.ToString("yyyy-MM-ddTHH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture));
    }
}
