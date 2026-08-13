using System.Text.Json;
using System.Text.Json.Serialization;
using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

public sealed class ActivitySignalDetailJsonConverter : JsonConverter<IActivitySignalDetail>
{
    public override IActivitySignalDetail Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("ConnectorSignalDetail is serialized for the model only and is never read back.");
    }

    public override void Write(Utf8JsonWriter writer, IActivitySignalDetail value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();
        JsonSerializer.Serialize(writer, value, runtimeType, options);
    }
}
