using ErrorOr;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnector(
    IntegrationKey key,
    Func<ActivityFetchContainer, CancellationToken, Task<ErrorOr<IReadOnlyList<ActivitySignal>>>> fetch,
    Func<DetailQuery, CancellationToken, Task<ErrorOr<IReadOnlyList<DetailField>>>>? details = null) : IConnector
{
    public IntegrationKey Key { get; } = key;

    public int FetchCount { get; private set; }

    public int DetailCount { get; private set; }

    public Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);

    public Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(string userId, ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        FetchCount++;
        return fetch(container, cancellationToken);
    }

    public Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(string userId, DetailQuery query, CancellationToken cancellationToken)
    {
        DetailCount++;

        if (details is null)
        {
            IReadOnlyList<DetailField> empty = [];
            return Task.FromResult(empty.ToErrorOr());
        }

        return details(query, cancellationToken);
    }
}
