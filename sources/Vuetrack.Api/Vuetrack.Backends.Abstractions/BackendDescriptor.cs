namespace Vuetrack.Backends.Abstractions;

public sealed record BackendDescriptor
{
    public required BackendKey Key { get; init; }

    public required string DisplayName { get; init; }

    public required BackendCapabilities Capabilities { get; init; }
}
