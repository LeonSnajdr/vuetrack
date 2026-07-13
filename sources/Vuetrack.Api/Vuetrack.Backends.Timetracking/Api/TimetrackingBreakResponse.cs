namespace Vuetrack.Backends.Timetracking.Api;

public sealed class TimetrackingBreakResponse
{
    public long DurationMillis { get; set; }

    public bool Valid { get; set; }
}
