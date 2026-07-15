namespace Vuetrack.Api.Features.Suggestions.Engine.Rules;

// A deterministic policy that turns one normalized signal into a bounded, weighted candidate interval.
// Rules are evaluated in order; the first that applies wins.
public interface IEvidenceRule
{
    bool AppliesTo(NormalizedSignal signal);

    CandidateEvidence Create(NormalizedSignal signal, SuggestionEngineOptions options);
}
