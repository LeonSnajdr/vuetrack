namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionContract(
    string Id,
    string? TaskId,
    string? ProjectId,
    string? ActivityId,
    DateTime DateStarted,
    DateTime DateEnded,
    string? Comment,
    string Status,
    IReadOnlyList<SuggestionEvidenceContract> Sources,
    double Confidence);
