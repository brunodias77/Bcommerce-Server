using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthService.Api.Converters;

/// <summary>
/// Conversor personalizado para DateTime que garante que datas sem timezone sejam tratadas como UTC
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (DateTime.TryParse(dateString, out var date))
        {
            // Se a data não tem informação de timezone, assumir UTC
            return date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date;
        }
        throw new JsonException($"Unable to parse DateTime: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>
/// Conversor personalizado para DateTime? que garante que datas sem timezone sejam tratadas como UTC
/// </summary>
public class NullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
            
        var dateString = reader.GetString();
        if (DateTime.TryParse(dateString, out var date))
        {
            // Se a data não tem informação de timezone, assumir UTC
            return date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date;
        }
        throw new JsonException($"Unable to parse DateTime: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        else
            writer.WriteNullValue();
    }
}