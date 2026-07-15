namespace Vuetrack.Api.Features.Suggestions.Engine;

// The bounded, weighted interval an evidence rule derives from a single normalized signal.
public sealed record CandidateEvidence
{
    public required NormalizedSignal Signal { get; init; }

    public required DateTime DateStarted { get; init; }

    public required DateTime DateEnded { get; init; }

    public required double Weight { get; init; }

    public required bool HasExplicitDuration { get; init; }

    // Whether this evidence can promote to a suggestion on its own (e.g. an explicit worklog),
    // as opposed to only corroborating other evidence.
    public required bool CanCreateAlone { get; init; }
}
