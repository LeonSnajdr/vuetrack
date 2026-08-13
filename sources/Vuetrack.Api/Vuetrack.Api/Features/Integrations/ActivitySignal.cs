namespace Vuetrack.Api.Features.Integrations;

public sealed record ActivitySignal
{
    public required IntegrationKey Key { get; init; }

    public required string ExternalId { get; init; }

    public required DateTime DateStarted { get; init; }

    public DateTime? DateEnded { get; init; }

    public ActivityKind Kind { get; init; } = ActivityKind.Unknown;

    public required IActivitySignalDetail Detail { get; init; }
}
