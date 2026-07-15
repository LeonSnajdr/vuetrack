using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine.Rules;

// Commits: a short bounded focus window; corroborating, cannot create a suggestion alone.
public sealed class CommitRule : IEvidenceRule
{
    public bool AppliesTo(NormalizedSignal signal)
    {
        return signal.Kind is ActivityKind.Commit;
    }

    public CandidateEvidence Create(NormalizedSignal signal, SuggestionEngineOptions options)
    {
        var end = signal.DateStarted + options.CommitWindow;

        return new CandidateEvidence
        {
            Signal = signal,
            DateStarted = signal.DateStarted,
            DateEnded = end,
            Weight = options.CommitWeight,
            HasExplicitDuration = false,
            CanCreateAlone = false,
        };
    }
}
