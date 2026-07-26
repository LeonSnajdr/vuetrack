using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity.Api;

namespace Vuetrack.Connectors.Jira.Activity;

public static class JiraActivityClassifier
{
    public static ActivityKind Classify(this JiraChangelogItemResponse item)
    {
        var field = item.FieldId ?? item.Field;
        var normalized = field?.ToLowerInvariant();

        return normalized switch
        {
            "status" => ActivityKind.StatusTransition,
            "summary" or "description" => ActivityKind.ContentEdit,
            "priority" or "assignee" or "resolution" or "reporter" or "labels" or "parent" => ActivityKind.Planning,
            "project" or "key" => ActivityKind.Admin,
            _ => ActivityKind.Planning,
        };
    }
}
