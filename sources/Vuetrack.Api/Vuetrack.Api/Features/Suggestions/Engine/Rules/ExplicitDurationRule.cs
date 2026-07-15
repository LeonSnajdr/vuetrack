using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine.Rules;

// Explicit worklogs and calls: strong evidence with a real duration that can stand on its own.
public sealed class ExplicitDurationRule : IEvidenceRule
{
    public bool AppliesTo(NormalizedSignal signal)
    {
        return signal.HasExplicitDuration || signal.Kind is ActivityKind.Worklog or ActivityKind.Call;
    }

    public CandidateEvidence Create(NormalizedSignal signal, SuggestionEngineOptions options)
    {
        var end = signal.DateEnded ?? signal.DateStarted + options.CorroborationWindow;

        return new CandidateEvidence
        {
            Signal = signal,
            DateStarted = signal.DateStarted,
            DateEnded = end,
            Weight = options.ExplicitWeight,
            HasExplicitDuration = signal.HasExplicitDuration,
            CanCreateAlone = true,
        };
    }
}
