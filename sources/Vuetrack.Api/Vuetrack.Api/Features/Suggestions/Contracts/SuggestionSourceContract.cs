namespace Vuetrack.Api.Features.Suggestions.Contracts;

public sealed record SuggestionSourceContract(string ConnectorKey, string ExternalId, string? Link);
