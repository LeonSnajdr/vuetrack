using System.Text.Json;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine;

// One contributing source behind a suggestion, with the normalized interval, activity kind, weight,
// and the original raw metadata preserved for explainability and later comparison.
public sealed record SuggestionEvidence(
    ConnectorKey ConnectorKey,
    string ExternalId,
    ActivityKind Kind,
    DateTime DateStarted,
    DateTime DateEnded,
    double Weight,
    IReadOnlyDictionary<string, JsonElement> Metadata);
