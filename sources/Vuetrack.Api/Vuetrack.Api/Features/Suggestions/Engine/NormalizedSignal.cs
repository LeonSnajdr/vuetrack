using System.Text.Json;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Api.Features.Suggestions.Engine;

// A validated ActivitySignal projected into typed engine facts. The engine works only against these
// fields plus the retained raw metadata; it never reads connector-specific keys directly.
public sealed record NormalizedSignal
{
    public required ConnectorKey ConnectorKey { get; init; }

    public required string ExternalId { get; init; }

    public required ActivityKind Kind { get; init; }

    public string? SubjectWorkItemId { get; init; }

    public string? ActorId { get; init; }

    public string? DisplayTitle { get; init; }

    public string? DisplayComment { get; init; }

    public string? DisplayProject { get; init; }

    public required IReadOnlyList<string> CorrelationKeys { get; init; }

    public required DateTime DateStarted { get; init; }

    public DateTime? DateEnded { get; init; }

    public bool HasExplicitDuration => DateEnded.HasValue;

    // The primary key events are grouped under.
    public required string PartitionKey { get; init; }

    public required IReadOnlyDictionary<string, JsonElement> Metadata { get; init; }
}
