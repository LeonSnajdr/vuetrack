namespace Vuetrack.Connectors.Abstractions;

public sealed record ActivitySignal
{
    public required ConnectorKey ConnectorKey { get; init; }

    public required string ExternalId { get; init; }

    public required DateTime DateStarted { get; init; }

    public DateTime? DateEnded { get; init; }

    public ActivityKind Kind { get; init; } = ActivityKind.Unknown;

    public required IConnectorSignalDetail Detail { get; init; }
}
