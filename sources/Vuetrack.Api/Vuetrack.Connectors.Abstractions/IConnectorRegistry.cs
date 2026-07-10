namespace Vuetrack.Connectors.Abstractions;

public interface IConnectorRegistry
{
    IReadOnlyList<ConnectorDescriptor> Descriptors { get; }

    IConnector? Resolve(string key);
}
