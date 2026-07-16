using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Abstractions;

public sealed class ActivityConnectorSignalDetailJsonConverter : JsonConverter<IConnectorSignalDetail>
{
    public override IConnectorSignalDetail Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("ConnectorSignalDetail is serialized for the model only and is never read back.");
    }

    public override void Write(Utf8JsonWriter writer, IConnectorSignalDetail value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();
        JsonSerializer.Serialize(writer, value, runtimeType, options);
    }
}
