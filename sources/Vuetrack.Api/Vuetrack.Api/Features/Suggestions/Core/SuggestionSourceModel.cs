using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core;

public sealed class SuggestionSourceModel
{
    [BsonRepresentation(BsonType.String)]
    public required ConnectorKey ConnectorKey { get; set; }

    public required string ExternalId { get; set; }

    public string? Link { get; set; }
}
