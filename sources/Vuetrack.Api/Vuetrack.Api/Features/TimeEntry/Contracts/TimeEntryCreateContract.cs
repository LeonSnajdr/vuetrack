namespace Vuetrack.Api.Features.TimeEntry.Contracts;

public sealed record TimeEntryCreateContract
{
    public string? TaskId { get; init; }

    public DateTime DateStarted { get; init; }

    public DateTime DateEnded { get; init; }

    public string ProjectId { get; init; } = string.Empty;

    public string ActivityId { get; init; } = string.Empty;

    public string? Comment { get; init; }
}
