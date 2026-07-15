using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record SuggestionEngineEvidence
{
    public required ConnectorKey ConnectorKey { get; init; }

    public required string ExternalId { get; init; }

    public ActivityKind Kind { get; init; }

    public DateTime DateStarted { get; init; }

    public DateTime DateEnded { get; init; }

    public double Confidence { get; init; }
}
