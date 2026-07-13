namespace Vuetrack.Backends.Abstractions;

public sealed record DateRange
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}
