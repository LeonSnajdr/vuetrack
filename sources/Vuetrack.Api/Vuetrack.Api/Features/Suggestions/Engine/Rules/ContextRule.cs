namespace Vuetrack.Api.Features.Suggestions.Engine.Rules;

// Planning/administrative changes and anything unclassified: context only. It may extend or bridge a
// nearby block but carries no weight, so it never promotes on its own.
public sealed class ContextRule : IEvidenceRule
{
    public bool AppliesTo(NormalizedSignal signal)
    {
        return true;
    }

    public CandidateEvidence Create(NormalizedSignal signal, SuggestionEngineOptions options)
    {
        var end = signal.DateStarted + options.CorroborationWindow;

        return new CandidateEvidence
        {
            Signal = signal,
            DateStarted = signal.DateStarted,
            DateEnded = end,
            Weight = options.ContextWeight,
            HasExplicitDuration = false,
            CanCreateAlone = false,
        };
    }
}
