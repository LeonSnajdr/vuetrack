using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Infrastructure.Connectors;

[Inject]
public class ConnectorRegistry(IEnumerable<ISuggestionConnector> connectors) : IConnectorRegistry
{
    private IReadOnlyList<ISuggestionConnector> Connectors { get; } = connectors.ToList();

    public IReadOnlyList<ConnectorDescriptor> Descriptors =>
        Connectors.Select(connector => connector.Descriptor).ToList();

    public ISuggestionConnector? Resolve(string key) =>
        Connectors.FirstOrDefault(connector => connector.Descriptor.Key == key);
}
