namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionContract(
    string Id,
    string Title,
    string? Description,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Status,
    IReadOnlyList<SuggestionSourceContract> Sources,
    double Confidence,
    string? ProposedBackendProjectId,
    string? ProposedBackendActivityId);
