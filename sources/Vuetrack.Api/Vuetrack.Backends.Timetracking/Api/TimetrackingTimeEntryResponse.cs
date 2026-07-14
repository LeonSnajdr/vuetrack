namespace Vuetrack.Backends.Timetracking.Api;

public sealed class TimetrackingTimeEntryResponse
{
    public long? TimeEntryId { get; set; }

    public long UserId { get; set; }

    public long? CreatedByUserId { get; set; }

    public TimetrackingActivityResponse Project { get; set; } = new();

    public TimetrackingActivityResponse Activity { get; set; } = new();

    public TimetrackingBreakResponse? BreakDetails { get; set; }

    public string TaskId { get; set; } = string.Empty;

    public string StartDate { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public bool Approved { get; set; }
}
