namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionContract(
    string Id,
    string Title,
    string? TaskId,
    string? ProjectId,
    string? ActivityId,
    DateTime DateStarted,
    DateTime DateEnded,
    string? Comment,
    string Status,
    IReadOnlyList<SuggestionSourceContract> Sources,
    double Confidence);
