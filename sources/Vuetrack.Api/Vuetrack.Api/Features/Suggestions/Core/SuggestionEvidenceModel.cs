using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Suggestions.Core;

public sealed class SuggestionEvidenceModel
{
    [BsonRepresentation(BsonType.String)]
    public required IntegrationKey Key { get; set; }

    public required string ExternalId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ActivityKind Kind { get; set; }

    public DateTime DateStarted { get; set; }

    public DateTime DateEnded { get; set; }

    public double Confidence { get; set; }
}
