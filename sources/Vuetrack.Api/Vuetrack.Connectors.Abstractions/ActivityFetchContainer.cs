namespace Vuetrack.Connectors.Abstractions;

public sealed record ActivityFetchContainer
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}
