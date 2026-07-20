namespace Vuetrack.Connectors.Abstractions;

public sealed record ConnectorDescriptor
{
    public required ConnectorKey Key { get; init; }

    public required IReadOnlyList<ConnectorCapabilities> Capabilities { get; init; }
}
