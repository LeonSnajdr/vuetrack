using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record SuggestionEngineEvidence
{
    public required IntegrationKey Key { get; init; }

    public required string ExternalId { get; init; }

    public ActivityKind Kind { get; init; }

    public DateTime DateStarted { get; init; }

    public DateTime DateEnded { get; init; }

    public double Confidence { get; init; }
}
