namespace Vuetrack.Api.Features.Suggestions.Core;

public abstract record SuggestionDismissResult;

public sealed record SuggestionDismissed : SuggestionDismissResult;

public sealed record SuggestionDismissNotFound : SuggestionDismissResult;
