using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;

namespace Vuetrack.Connectors.Abstractions.Metadata;

// A serializable bag of typed metadata values. Values are stored as JsonElement so any connector
// fact can be preserved, but every read/write goes through a declared MetadataKey<T> so the engine
// works against typed facts instead of ad-hoc string parsing.
public sealed class SignalMetadata
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public SignalMetadata(IReadOnlyDictionary<string, JsonElement> values)
    {
        Values = values;
    }

    public static SignalMetadata Empty { get; } = new(new Dictionary<string, JsonElement>());

    public IReadOnlyDictionary<string, JsonElement> Values { get; }

    public bool Contains<T>(MetadataKey<T> key)
    {
        return Values.ContainsKey(key.Name);
    }

    public bool TryGet<T>(MetadataKey<T> key, out T? value)
    {
        if (!Values.TryGetValue(key.Name, out var element))
        {
            value = default;
            return false;
        }

        value = element.Deserialize<T>(SerializerOptions);
        return value is not null;
    }

    public ErrorOr<T> GetRequired<T>(MetadataKey<T> key)
    {
        if (!Values.TryGetValue(key.Name, out var element))
        {
            var missing = Error.Validation("Metadata.Missing", $"Required metadata '{key.Name}' is missing.");
            return missing;
        }

        var value = element.Deserialize<T>(SerializerOptions);
        if (value is null)
        {
            var invalid = Error.Validation("Metadata.Invalid", $"Metadata '{key.Name}' could not be read as {typeof(T).Name}.");
            return invalid;
        }

        return value;
    }
}
