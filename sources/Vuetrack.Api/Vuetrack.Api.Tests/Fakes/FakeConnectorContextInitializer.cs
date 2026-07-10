using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnectorContextInitializer(string connectorKey, bool connected) : IConnectorContextInitializer
{
    public string ConnectorKey { get; } = connectorKey;

    public Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(connected);
}
