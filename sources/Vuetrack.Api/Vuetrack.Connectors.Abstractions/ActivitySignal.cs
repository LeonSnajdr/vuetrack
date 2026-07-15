using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Connectors.Abstractions;

// Universal transport envelope for a connector event. Only the connector-agnostic transport fields
// are first class; every other fact (title, url, work-item identity, activity kind, ...) lives in
// typed Metadata so the engine never depends on connector-specific field names.
public sealed record ActivitySignal
{
    public required ConnectorKey ConnectorKey { get; init; }

    public required string ExternalId { get; init; }

    public required DateTime DateStarted { get; init; }

    public DateTime? DateEnded { get; init; }

    public SignalMetadata Metadata { get; init; } = SignalMetadata.Empty;
}
