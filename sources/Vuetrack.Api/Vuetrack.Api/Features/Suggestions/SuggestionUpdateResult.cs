using Vuetrack.Api.Features.Suggestions.Contracts;

namespace Vuetrack.Api.Features.Suggestions;

public abstract record SuggestionUpdateResult;

public sealed record SuggestionUpdated(SuggestionContract Suggestion) : SuggestionUpdateResult;

public sealed record SuggestionNotFound : SuggestionUpdateResult;
