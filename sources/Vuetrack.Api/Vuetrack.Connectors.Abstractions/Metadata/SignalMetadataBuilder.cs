using System.Text.Json;

namespace Vuetrack.Connectors.Abstractions.Metadata;

// Fluent builder connectors use to assemble a SignalMetadata. Each Set serializes the value through
// its declared key; keys omitted here are simply absent and fail GetRequired at the validation boundary.
public sealed class SignalMetadataBuilder
{
    private readonly Dictionary<string, JsonElement> values = new();

    public SignalMetadataBuilder Set<T>(MetadataKey<T> key, T value)
    {
        var element = JsonSerializer.SerializeToElement(value, SignalMetadata.SerializerOptions);
        values[key.Name] = element;
        return this;
    }

    public SignalMetadataBuilder SetIfNotNull<T>(MetadataKey<T> key, T? value)
        where T : class
    {
        if (value is null)
        {
            return this;
        }

        return Set(key, value);
    }

    public SignalMetadata Build()
    {
        var snapshot = new Dictionary<string, JsonElement>(values);
        return new SignalMetadata(snapshot);
    }
}
