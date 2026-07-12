using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionSourceContract(ConnectorKey ConnectorKey, string ExternalId, string? Link);
