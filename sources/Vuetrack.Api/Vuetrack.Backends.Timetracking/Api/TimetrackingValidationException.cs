namespace Vuetrack.Backends.Timetracking.Api;

public sealed class TimetrackingValidationException(IReadOnlyList<TimetrackingFieldError> errors) : Exception("Timetracking rejected the time entry")
{
    public IReadOnlyList<TimetrackingFieldError> Errors { get; } = errors;
}

public sealed record TimetrackingFieldError(string Field);
