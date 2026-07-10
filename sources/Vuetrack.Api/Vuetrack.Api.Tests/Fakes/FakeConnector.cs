using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnector(ConnectorDescriptor descriptor, Func<ActivityFetchContainer, CancellationToken, Task<ActivityFetchResult>> fetch) : IConnector
{
    public ConnectorDescriptor Descriptor { get; } = descriptor;

    public Task<ConnectorValidationResult> ValidateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ConnectorValidationResult>(new ConnectorValidationValid());

    public Task<ActivityFetchResult> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken) =>
        fetch(container, cancellationToken);
}
