using ErrorOr;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnector(ConnectorDescriptor descriptor, Func<ActivityFetchContainer, CancellationToken, Task<ErrorOr<IReadOnlyList<ActivitySignal>>>> fetch) : IConnector
{
    public ConnectorDescriptor Descriptor { get; } = descriptor;

    public Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);

    public Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken) =>
        fetch(container, cancellationToken);
}
