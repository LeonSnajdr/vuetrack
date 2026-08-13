using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

// Jira Cloud REST v3 returns timestamps with a colonless RFC822 offset (e.g. "2024-01-15T10:30:00.000+0000").
// System.Text.Json's built-in ISO 8601 parser rejects that offset form, so we parse with the lenient BCL parser.
public sealed class JiraDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // Jira's changelog bulkfetch returns "created" as epoch milliseconds (a JSON number),
        // while other endpoints return ISO strings.
        if (reader.TokenType == JsonTokenType.Number)
        {
            var epochMilliseconds = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds).UtcDateTime;
        }

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parsed = DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value);
        if (!parsed)
        {
            throw new JsonException($"Could not parse '{text}' as DateTime.");
        }

        return value.UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
