using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnectorContextInitializer(ConnectorKey connectorKey, bool connected) : IConnectorContextInitializer
{
    public ConnectorKey ConnectorKey { get; } = connectorKey;

    public Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(connected);
}
