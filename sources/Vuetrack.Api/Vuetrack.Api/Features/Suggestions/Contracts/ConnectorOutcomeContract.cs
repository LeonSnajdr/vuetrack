namespace Vuetrack.Api.Features.Suggestions.Contracts;

public sealed record ConnectorOutcomeContract(string ConnectorKey, string Status, int SignalCount, string? Message);
