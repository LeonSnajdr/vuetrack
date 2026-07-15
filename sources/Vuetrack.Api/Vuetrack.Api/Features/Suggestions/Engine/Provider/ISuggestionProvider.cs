using ErrorOr;

namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

public interface ISuggestionProvider
{
    Task<ErrorOr<IReadOnlyList<SuggestionProviderCandidate>>> ProvideAsync(SuggestionProviderContext context, CancellationToken cancellationToken);
}
