namespace Vuetrack.Backends.Abstractions.Contracts;

public sealed record TimeEntryContract
{
    public string? Id { get; init; }

    public required string UserId { get; init; }

    public string? TaskId { get; init; }

    public required ProjectContract Project { get; init; }

    public required ActivityContract Activity { get; init; }

    public TimeEntryBreakContract? BreakDetails { get; init; }

    public required DateTime DateStarted { get; init; }

    public required DateTime DateEnded { get; init; }

    public string? Comment { get; init; }
}
