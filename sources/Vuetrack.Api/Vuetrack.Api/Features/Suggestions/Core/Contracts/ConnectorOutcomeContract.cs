namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record ConnectorOutcomeContract(string ConnectorKey, string Status, int SignalCount, string? Message);
