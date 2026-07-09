namespace Vuetrack.Connectors.Abstractions;

public interface IConnectorRegistry
{
    IReadOnlyList<ConnectorDescriptor> Descriptors { get; }

    ISuggestionConnector? Resolve(string key);
}
