using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine;

public sealed record SignalRef(ConnectorKey ConnectorKey, string ExternalId, string? Link);
