namespace Vuetrack.Connectors.Abstractions;

public sealed record ActivitySignal
{
    public required string ConnectorKey { get; init; }

    public required string ExternalId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTime Start { get; init; }

    public DateTime? End { get; init; }

    public string? Link { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
