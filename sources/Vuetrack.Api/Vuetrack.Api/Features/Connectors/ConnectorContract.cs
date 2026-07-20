using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Connectors;

public sealed record ConnectorContract(ConnectorKey Key, IReadOnlyList<ConnectorCapabilities> Capabilities, bool Connected, bool Healthy);
