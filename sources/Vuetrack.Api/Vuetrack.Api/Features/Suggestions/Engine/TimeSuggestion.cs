using System.Text.Json;

namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record TimeSuggestion(
    string? TaskId,
    string? ProjectName,
    string? Comment,
    DateTime DateStarted,
    DateTime DateEnded,
    double Confidence,
    IReadOnlyDictionary<string, JsonElement> CanonicalMetadata,
    IReadOnlyList<SuggestionEvidence> Sources);
