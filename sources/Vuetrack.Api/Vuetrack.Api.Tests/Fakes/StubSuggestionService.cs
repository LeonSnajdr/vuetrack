using Vuetrack.Api.Features.Suggestions;
using Vuetrack.Api.Features.Suggestions.Contracts;
using Vuetrack.Api.Features.Suggestions.Services;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class StubSuggestionService : ISuggestionService
{
    public Func<string, string, SuggestionUpdateContract, CancellationToken, Task<SuggestionUpdateResult>>? OnUpdate { get; set; }

    public Func<string, string, CancellationToken, Task<SuggestionDismissResult>>? OnDismiss { get; set; }

    public Task<GenerateSuggestionsResultContract> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTimeOffset from, DateTimeOffset to) =>
        throw new NotImplementedException();

    public Task<SuggestionUpdateResult> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken) =>
        OnUpdate!.Invoke(userId, id, request, cancellationToken);

    public Task<SuggestionDismissResult> DismissAsync(string userId, string id, CancellationToken cancellationToken) =>
        OnDismiss!.Invoke(userId, id, cancellationToken);
}
