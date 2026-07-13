using System.Globalization;
using Vuetrack.Backends.Abstractions.Contracts;
using Vuetrack.Backends.Timetracking.Api;

namespace Vuetrack.Backends.Timetracking.Internal;

/// <summary>
/// Maps between the shared Vuetrack contracts (combined <see cref="DateTime"/>, string ids) and the
/// legacy timetracking wire shape (split German date/time strings, numeric ids, dotted form keys).
/// </summary>
internal static class TimetrackingMapper
{
    private const string DateFormat = "dd.MM.yyyy";
    private const string TimeFormat = "HH:mm";

    public static string FormatDate(DateTime value) => value.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static TimeEntryContract ToContract(this TimetrackingTimeEntryResponse response)
    {
        return new TimeEntryContract
        {
            Id = response.TimeEntryId?.ToString(CultureInfo.InvariantCulture),
            UserId = response.UserId.ToString(CultureInfo.InvariantCulture),
            TaskId = string.IsNullOrWhiteSpace(response.TaskId) ? null : response.TaskId,
            Project = new ProjectContract(response.Project.Id.ToString(CultureInfo.InvariantCulture), response.Project.Name),
            Activity = new ActivityContract(response.Activity.Id.ToString(CultureInfo.InvariantCulture), response.Activity.Name),
            BreakDetails = response.BreakDetails is null ? null : new TimeEntryBreakContract(response.BreakDetails.DurationMillis, response.BreakDetails.Valid),
            DateStarted = Combine(response.StartDate, response.StartTime),
            DateEnded = Combine(response.EndDate, response.EndTime),
            Comment = string.IsNullOrEmpty(response.Comment) ? null : response.Comment,
        };
    }

    public static ProjectContract ToProjectContract(this TimetrackingRefResponse response)
    {
        return new ProjectContract(response.Id.ToString(CultureInfo.InvariantCulture), response.Name);
    }

    public static ActivityContract ToActivityContract(this TimetrackingRefResponse response)
    {
        return new ActivityContract(response.Id.ToString(CultureInfo.InvariantCulture), response.Name);
    }

    public static Dictionary<string, string> ToCreateForm(TimeEntryCreateContract contract, string? externalUserId)
    {
        return BuildForm(null, contract.TaskId, contract.ProjectId, contract.ActivityId, contract.DateStarted, contract.DateEnded, contract.Comment, externalUserId);
    }

    public static Dictionary<string, string> ToUpdateForm(string id, TimeEntryUpdateContract contract, string? externalUserId)
    {
        return BuildForm(id, contract.TaskId, contract.ProjectId, contract.ActivityId, contract.DateStarted, contract.DateEnded, contract.Comment, externalUserId);
    }

    private static Dictionary<string, string> BuildForm(string? id, string? taskId, string projectId, string activityId, DateTime start, DateTime end, string? comment, string? externalUserId)
    {
        // Legacy /api/v1/timeEntry/upsert binds a Play form: nested project/activity via dotted keys,
        // split date/time, and a required user id for the server-side validation context.
        var form = new Dictionary<string, string>
        {
            ["project.id"] = projectId,
            ["activity.id"] = activityId,
            ["taskId"] = taskId ?? string.Empty,
            ["startDate"] = start.ToString(DateFormat, CultureInfo.InvariantCulture),
            ["startTime"] = start.ToString(TimeFormat, CultureInfo.InvariantCulture),
            ["endDate"] = end.ToString(DateFormat, CultureInfo.InvariantCulture),
            ["endTime"] = end.ToString(TimeFormat, CultureInfo.InvariantCulture),
            ["comment"] = comment ?? string.Empty,
            ["approved"] = "false",
            ["billable"] = "false",
            ["invoiceInfo"] = string.Empty,
        };

        if (!string.IsNullOrEmpty(id))
        {
            form["timeEntryId"] = id;
        }

        if (!string.IsNullOrEmpty(externalUserId))
        {
            form["userId"] = externalUserId;
        }

        return form;
    }

    private static DateTime Combine(string date, string time)
    {
        var text = string.IsNullOrWhiteSpace(time) ? date : $"{date} {time}";
        var format = string.IsNullOrWhiteSpace(time) ? DateFormat : $"{DateFormat} {TimeFormat}";

        return DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : default;
    }
}
