using ErrorOr;
using Vuetrack.Api.Features.Suggestions.Engine.Provider;

namespace Vuetrack.Api.Tests.Fakes;

// A deterministic stand-in for the real provider: it emits exactly one candidate per signal, echoing
// the signal's own FakeSignalDetail back. Lets the service-level tests exercise aggregation and dedup
// without a real provider, mirroring how the old rule engine promoted each worklog into its own suggestion.
public sealed class EchoSuggestionProvider : ISuggestionProvider
{
    public Task<ErrorOr<IReadOnlyList<SuggestionProviderCandidate>>> ProvideAsync(SuggestionProviderContext context, CancellationToken cancellationToken)
    {
        var candidates = new List<SuggestionProviderCandidate>();

        foreach (var signal in context.Signals)
        {
            var detail = signal.Detail as FakeSignalDetail;

            var end = signal.DateEnded ?? signal.DateStarted.AddMinutes(15);
            var candidate = new SuggestionProviderCandidate
            {
                TaskId = detail?.TaskId,
                Comment = detail?.Comment,
                DateStarted = signal.DateStarted,
                DateEnded = end,
                Confidence = 1.0,
                SourceExternalIds = [signal.ExternalId],
            };

            candidates.Add(candidate);
        }

        IReadOnlyList<SuggestionProviderCandidate> result = candidates;
        return Task.FromResult(result.ToErrorOr());
    }
}
