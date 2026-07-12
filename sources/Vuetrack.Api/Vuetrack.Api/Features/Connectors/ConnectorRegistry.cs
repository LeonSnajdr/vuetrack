using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Connectors;

[Inject]
public class ConnectorRegistry(IEnumerable<IConnector> connectors) : IConnectorRegistry
{
    private IReadOnlyList<IConnector> Connectors { get; } = connectors.ToList();

    public IReadOnlyList<ConnectorDescriptor> Descriptors => Connectors.Select(connector => connector.Descriptor).ToList();

    public IConnector? Resolve(ConnectorKey key) => Connectors.FirstOrDefault(connector => connector.Descriptor.Key == key);
}
