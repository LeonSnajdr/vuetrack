namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

public sealed record SuggestionProviderCandidate
{
    public string? TaskId { get; init; }

    public string? Comment { get; init; }

    public required DateTime DateStarted { get; init; }

    public required DateTime DateEnded { get; init; }

    public double Confidence { get; init; }

    public required IReadOnlyList<string> SourceExternalIds { get; init; }
}
