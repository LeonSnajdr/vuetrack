namespace Vuetrack.Connectors.Abstractions;

public sealed record ActivitySignal
{
    public required ConnectorKey ConnectorKey { get; init; }

    public required string ExternalId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTime DateStarted { get; init; }

    public DateTime? DateEnded { get; init; }

    public string? Link { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public static class ActivityMetadataKeys
{
    public const string TaskId = "taskId";
}
