using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine.Rules;

// User-authored Jira status transitions: a short bounded focus window. Moving an issue between states
// is a strong signal that the user worked on it, so it can promote to a suggestion on its own.
public sealed class StatusTransitionRule : IEvidenceRule
{
    public bool AppliesTo(NormalizedSignal signal)
    {
        return signal.Kind is ActivityKind.StatusTransition;
    }

    public CandidateEvidence Create(NormalizedSignal signal, SuggestionEngineOptions options)
    {
        var end = signal.DateStarted + options.CorroborationWindow;

        return new CandidateEvidence
        {
            Signal = signal,
            DateStarted = signal.DateStarted,
            DateEnded = end,
            Weight = options.CorroborationWeight,
            HasExplicitDuration = false,
            CanCreateAlone = true,
        };
    }
}
