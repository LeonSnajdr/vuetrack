using ErrorOr;
using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

[InjectAs(typeof(ISuggestionProvider))]
public sealed class SuggestionProvider(ILogger<SuggestionProvider> logger) : ISuggestionProvider
{
    private ILogger<SuggestionProvider> Logger { get; } = logger;

    public Task<ErrorOr<IReadOnlyList<SuggestionProviderCandidate>>> ProvideAsync(SuggestionProviderContext context, CancellationToken cancellationToken)
    {
        Logger.LogWarning("Suggestion provider is not configured; returning no suggestions for {SignalCount} signals in [{From}, {To}]", context.Signals.Count, context.From, context.To);

        IReadOnlyList<SuggestionProviderCandidate> empty = [];
        var result = empty.ToErrorOr();
        return Task.FromResult(result);
    }
}
