using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine.Rules;

// User-authored comments, content edits, or issue creation: short bounded, corroborating evidence.
public sealed class CorroboratingRule : IEvidenceRule
{
    public bool AppliesTo(NormalizedSignal signal)
    {
        return signal.Kind is ActivityKind.Comment or ActivityKind.ContentEdit or ActivityKind.IssueCreated;
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
            CanCreateAlone = false,
        };
    }
}
