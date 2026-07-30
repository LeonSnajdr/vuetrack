using ErrorOr;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnector(
    ConnectorDescriptor descriptor,
    Func<ActivityFetchContainer, CancellationToken, Task<ErrorOr<IReadOnlyList<ActivitySignal>>>> fetch,
    Func<DetailQuery, CancellationToken, Task<ErrorOr<IReadOnlyList<DetailField>>>>? details = null) : IConnector
{
    public ConnectorDescriptor Descriptor { get; } = descriptor;

    public int FetchCount { get; private set; }

    public int DetailCount { get; private set; }

    public Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);

    public Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        FetchCount++;
        return fetch(container, cancellationToken);
    }

    public Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(DetailQuery query, CancellationToken cancellationToken)
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
