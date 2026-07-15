using System.Text.Json;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionContract(
    string Id,
    string? TaskId,
    string? ProjectId,
    string? ProjectName,
    string? ActivityId,
    DateTime DateStarted,
    DateTime DateEnded,
    string? Comment,
    string Status,
    IReadOnlyDictionary<string, JsonElement> Metadata,
    IReadOnlyList<SuggestionEvidenceContract> Sources,
    double Confidence);
