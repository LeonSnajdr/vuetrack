using ErrorOr;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnector(
    IntegrationKey key,
    Func<DateRange, CancellationToken, Task<ErrorOr<IReadOnlyList<ActivitySignal>>>> fetch,
    Func<DetailQuery, CancellationToken, Task<ErrorOr<IReadOnlyList<DetailField>>>>? details = null) : IConnector
{
    public IntegrationKey Key { get; } = key;

    public int FetchCount { get; private set; }

    public int DetailCount { get; private set; }

    public Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);

    public Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(string userId, DateRange range, CancellationToken cancellationToken)
    {
        FetchCount++;
        return fetch(range, cancellationToken);
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
