namespace Vuetrack.Api.Features.Suggestions.Contracts;

public sealed record GenerateSuggestionsResultContract(int GeneratedCount, IReadOnlyList<ConnectorOutcomeContract> ConnectorOutcomes);
