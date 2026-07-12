namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionContract(
    string Id,
    string Title,
    string? Description,
    DateTime DateStarted,
    DateTime DateEnded,
    string Status,
    IReadOnlyList<SuggestionSourceContract> Sources,
    double Confidence,
    string? ProposedBackendProjectId,
    string? ProposedBackendActivityId);
