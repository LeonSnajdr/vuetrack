using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeConnectorRegistry : IConnectorRegistry
{
    private readonly Dictionary<ConnectorKey, IConnector> connectors = [];

    public IReadOnlyList<ConnectorDescriptor> Descriptors => connectors.Values.Select(c => c.Descriptor).ToList();

    public void Add(IConnector connector) => connectors[connector.Descriptor.Key] = connector;

    public IConnector? Resolve(ConnectorKey key) => connectors.GetValueOrDefault(key);
}
