namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionSourceContract(string ConnectorKey, string ExternalId, string? Link);
