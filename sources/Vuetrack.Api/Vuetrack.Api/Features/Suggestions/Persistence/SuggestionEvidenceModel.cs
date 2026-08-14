using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Features.Suggestions.Persistence;

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
