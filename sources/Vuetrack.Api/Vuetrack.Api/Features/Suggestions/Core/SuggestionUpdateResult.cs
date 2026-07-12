using Vuetrack.Api.Features.Suggestions.Core.Contracts;

namespace Vuetrack.Api.Features.Suggestions.Core;

public abstract record SuggestionUpdateResult;

public sealed record SuggestionUpdated(SuggestionContract Suggestion) : SuggestionUpdateResult;

public sealed record SuggestionNotFound : SuggestionUpdateResult;
