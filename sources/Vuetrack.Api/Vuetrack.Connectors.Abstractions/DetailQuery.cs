namespace Vuetrack.Connectors.Abstractions;

public sealed record DetailQuery
{
    public string? TaskId { get; init; }
}
