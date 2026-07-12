namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record GenerateSuggestionsResultContract(int GeneratedCount, IReadOnlyList<ConnectorOutcomeContract> ConnectorOutcomes);
