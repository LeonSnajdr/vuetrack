namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record TimeSuggestion(
    string Title,
    string? TaskId,
    string? Comment,
    DateTime DateStarted,
    DateTime DateEnded,
    double Confidence,
    IReadOnlyList<SignalRef> Sources);
