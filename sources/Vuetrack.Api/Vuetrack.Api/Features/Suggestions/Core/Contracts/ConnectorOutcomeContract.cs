using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record ConnectorOutcomeContract(ConnectorKey ConnectorKey, string Status, int SignalCount, string? Message);
