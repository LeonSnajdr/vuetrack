using System.Text.Json;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionEvidenceContract(
    ConnectorKey ConnectorKey,
    string ExternalId,
    ActivityKind Kind,
    DateTime DateStarted,
    DateTime DateEnded,
    double Weight,
    IReadOnlyDictionary<string, JsonElement> Metadata);
