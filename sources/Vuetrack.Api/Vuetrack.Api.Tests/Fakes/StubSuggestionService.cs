using ErrorOr;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Core.Services;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class StubSuggestionService : ISuggestionService
{
    public Func<string, string, SuggestionUpdateContract, CancellationToken, Task<ErrorOr<SuggestionContract>>>? OnUpdate { get; set; }

    public Func<string, string, CancellationToken, Task<ErrorOr<Deleted>>>? OnDismiss { get; set; }

    public Func<string, string, CancellationToken, Task<ErrorOr<Success>>>? OnAccept { get; set; }

    public Func<string, GenerateSuggestionsRequestContract, CancellationToken, Task<ErrorOr<IReadOnlyList<SuggestionContract>>>>? OnGenerate { get; set; }

    public Func<string, GenerateSuggestionsRequestContract, CancellationToken, Task<ErrorOr<IReadOnlyList<SuggestionContract>>>>? OnReload { get; set; }

    public Task<ErrorOr<IReadOnlyList<SuggestionContract>>> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken) =>
        OnGenerate!.Invoke(userId, request, cancellationToken);

    public Task<ErrorOr<IReadOnlyList<SuggestionContract>>> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken) =>
        OnReload!.Invoke(userId, request, cancellationToken);

    public Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTime from, DateTime to) =>
        throw new NotImplementedException();

    public Task<ErrorOr<SuggestionContract>> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken) =>
        OnUpdate!.Invoke(userId, id, request, cancellationToken);

    public Task<ErrorOr<Deleted>> DismissAsync(string userId, string id, CancellationToken cancellationToken) =>
        OnDismiss!.Invoke(userId, id, cancellationToken);

    public Task<ErrorOr<Success>> AcceptAsync(string userId, string id, CancellationToken cancellationToken) =>
        OnAccept!.Invoke(userId, id, cancellationToken);
}
