namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record TimeSuggestion(
    string Title,
    string? Description,
    DateTimeOffset Start,
    DateTimeOffset End,
    double Confidence,
    IReadOnlyList<SignalRef> Sources);
