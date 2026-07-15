using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Core;

// Per-source evidence behind a persisted suggestion: which connector event contributed, its normalized
// interval, activity kind, weight, and the original raw metadata (as JSON) for explainability.
public sealed class SuggestionEvidenceModel
{
    [BsonRepresentation(BsonType.String)]
    public required ConnectorKey ConnectorKey { get; set; }

    public required string ExternalId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ActivityKind Kind { get; set; }

    public DateTime DateStarted { get; set; }

    public DateTime DateEnded { get; set; }

    public double Weight { get; set; }

    public string? MetadataJson { get; set; }
}
