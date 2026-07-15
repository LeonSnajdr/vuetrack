using System.Text.Json;

namespace Vuetrack.Api.Features.Suggestions.Core;

// Persists typed metadata dictionaries as a JSON string in Mongo and reads them back. JsonElement does
// not BSON-serialize cleanly, so metadata is stored as an opaque JSON document string and re-parsed on
// read; a round-trip test guards this boundary.
public static class SuggestionMetadataJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IReadOnlyDictionary<string, JsonElement> metadata)
    {
        var json = JsonSerializer.Serialize(metadata, Options);
        return json;
    }

    public static IReadOnlyDictionary<string, JsonElement> Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, JsonElement>();
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, Options);
        return parsed ?? new Dictionary<string, JsonElement>();
    }
}
