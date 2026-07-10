namespace Vuetrack.Connectors.Abstractions;

public sealed record ActivityFetchContainer
{
    public required DateTimeOffset From { get; init; }

    public required DateTimeOffset To { get; init; }
}
