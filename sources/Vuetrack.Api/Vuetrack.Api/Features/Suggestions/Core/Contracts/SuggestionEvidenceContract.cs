using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionEvidenceContract(
    ConnectorKey ConnectorKey,
    string ExternalId,
    ActivityKind Kind,
    DateTime DateStarted,
    DateTime DateEnded,
    double Weight);
