namespace Vuetrack.Connectors.Abstractions;

public sealed record ConnectorDescriptor
{
    public required string Key { get; init; }

    public required string DisplayName { get; init; }

    public required ConnectorCapabilities Capabilities { get; init; }

    public IReadOnlyList<ConfigField> ConfigSchema { get; init; } = [];
}
