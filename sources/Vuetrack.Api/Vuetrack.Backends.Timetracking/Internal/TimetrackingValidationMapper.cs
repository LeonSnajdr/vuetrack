using ErrorOr;
using Vuetrack.Backends.Abstractions.Contracts;
using Vuetrack.Backends.Timetracking.Api;

namespace Vuetrack.Backends.Timetracking.Internal;

internal static class TimetrackingValidationMapper
{
    private static readonly Dictionary<string, string> FieldMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["project.id"] = nameof(TimeEntryCreateContract.ProjectId),
        ["projectId"] = nameof(TimeEntryCreateContract.ProjectId),
        ["activity.id"] = nameof(TimeEntryCreateContract.ActivityId),
        ["activityId"] = nameof(TimeEntryCreateContract.ActivityId),
        ["taskId"] = nameof(TimeEntryCreateContract.TaskId),
        ["startDate"] = nameof(TimeEntryCreateContract.DateStarted),
        ["startTime"] = nameof(TimeEntryCreateContract.DateStarted),
        ["endDate"] = nameof(TimeEntryCreateContract.DateEnded),
        ["endTime"] = nameof(TimeEntryCreateContract.DateEnded),
        ["comment"] = nameof(TimeEntryCreateContract.Comment),
    };

    public static List<Error> ToValidationErrors(this TimetrackingValidationException exception)
    {
        var errors = new List<Error>();

        foreach (var fieldError in exception.Errors)
        {
            var field = MapField(fieldError.Field);
            var error = Error.Validation(field, nameof(ValidationError.Invalid));
            errors.Add(error);
        }

        if (errors.Count == 0)
        {
            var fallback = Error.Validation(string.Empty, nameof(ValidationError.Invalid));
            errors.Add(fallback);
        }

        return errors;
    }

    private static string MapField(string legacyField)
    {
        if (string.IsNullOrWhiteSpace(legacyField))
        {
            return string.Empty;
        }

        var mapped = FieldMap.GetValueOrDefault(legacyField, legacyField);
        return mapped;
    }
}
