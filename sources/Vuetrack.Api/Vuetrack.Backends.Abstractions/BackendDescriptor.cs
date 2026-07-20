namespace Vuetrack.Backends.Abstractions;

public sealed record BackendDescriptor
{
    public required BackendKey Key { get; init; }

    public required IReadOnlyList<BackendCapabilities> Capabilities { get; init; }
}
