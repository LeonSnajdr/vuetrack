namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record TimeSuggestion(
    string Title,
    string? Description,
    DateTime Start,
    DateTime End,
    double Confidence,
    IReadOnlyList<SignalRef> Sources);
