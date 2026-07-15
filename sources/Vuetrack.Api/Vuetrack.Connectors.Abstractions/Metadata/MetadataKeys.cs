using System.Collections.Generic;

namespace Vuetrack.Connectors.Abstractions.Metadata;

// Stable, connector-agnostic metadata keys shared by every connector and understood by the engine.
// Connector-owned keys (for example jira.*) are declared inside their own connector.
public static class MetadataKeys
{
    public static readonly MetadataKey<ActivityKind> ActivityKind = new("activity.kind");

    public static readonly MetadataKey<string> SubjectWorkItemId = new("subject.workItem.id");

    public static readonly MetadataKey<string> ActorId = new("actor.id");

    public static readonly MetadataKey<string> DisplayTitle = new("display.title");

    public static readonly MetadataKey<string> DisplayComment = new("display.comment");

    public static readonly MetadataKey<string> DisplayProject = new("display.project");

    public static readonly MetadataKey<string> SourceUrl = new("source.url");

    public static readonly MetadataKey<IReadOnlyList<string>> CorrelationKeys = new("correlation.keys");
}
