namespace Vuetrack.Connectors.Abstractions;

public enum ConfigFieldType
{
    Text,
    Url,
    Secret,
    Bool,
}

public sealed record ConfigField
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public required ConfigFieldType Type { get; init; }

    public bool Required { get; init; }

    public string? Description { get; init; }
}
