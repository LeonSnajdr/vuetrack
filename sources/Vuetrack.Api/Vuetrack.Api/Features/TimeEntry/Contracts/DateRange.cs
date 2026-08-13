namespace Vuetrack.Api.Features.TimeEntry.Contracts;

public sealed record DateRange
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}
