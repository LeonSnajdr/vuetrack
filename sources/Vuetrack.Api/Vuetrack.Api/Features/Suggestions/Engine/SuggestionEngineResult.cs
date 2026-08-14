namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record SuggestionEngineResult
{
    public string? TaskId { get; init; }

    public string? Comment { get; init; }

    public DateTime DateStarted { get; init; }

    public DateTime DateEnded { get; init; }

    public double Confidence { get; init; }

    public required IReadOnlyList<SuggestionEngineEvidence> Sources { get; init; }
}
