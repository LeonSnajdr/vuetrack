using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine;

// Projects a validated ActivitySignal into typed engine facts once, so the rest of the engine never
// touches string keys or connector-specific field names.
public static class SignalNormalizer
{
    public static NormalizedSignal Normalize(ActivitySignal signal)
    {
        var metadata = signal.Metadata;

        var kind = ActivityKind.Unknown;
        if (metadata.TryGet(MetadataKeys.ActivityKind, out var parsedKind))
        {
            kind = parsedKind;
        }

        metadata.TryGet(MetadataKeys.SubjectWorkItemId, out var subject);
        metadata.TryGet(MetadataKeys.ActorId, out var actor);
        metadata.TryGet(MetadataKeys.DisplayTitle, out var title);
        metadata.TryGet(MetadataKeys.DisplayComment, out var comment);
        metadata.TryGet(MetadataKeys.DisplayProject, out var project);

        var correlationKeys = ResolveCorrelationKeys(metadata, subject);
        var partitionKey = correlationKeys.Count > 0
            ? correlationKeys[0]
            : $"{signal.ConnectorKey}|{signal.ExternalId}";

        return new NormalizedSignal
        {
            ConnectorKey = signal.ConnectorKey,
            ExternalId = signal.ExternalId,
            Kind = kind,
            SubjectWorkItemId = subject,
            ActorId = actor,
            DisplayTitle = title,
            DisplayComment = comment,
            DisplayProject = project,
            CorrelationKeys = correlationKeys,
            DateStarted = signal.DateStarted,
            DateEnded = signal.DateEnded,
            PartitionKey = partitionKey,
            Metadata = metadata.Values,
        };
    }

    private static IReadOnlyList<string> ResolveCorrelationKeys(SignalMetadata metadata, string? subject)
    {
        if (metadata.TryGet(MetadataKeys.CorrelationKeys, out var keys) && keys is { Count: > 0 })
        {
            return keys;
        }

        if (!string.IsNullOrEmpty(subject))
        {
            return [subject];
        }

        return [];
    }
}
