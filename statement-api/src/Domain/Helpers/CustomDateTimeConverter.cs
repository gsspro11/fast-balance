using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Domain.Helpers;

public class CustomDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "dd/MM/yyyy HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        // Try parsing the custom format as a fallback
        if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        
        // Try parsing ISO 8601 format (default JSON datetime format)
        if (DateTime.TryParse(dateString, null, DateTimeStyles.RoundtripKind, out date))
        {
            return date; // Successfully parsed
        }

        throw new JsonException($"Unable to parse '{dateString}' as valid DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}