using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Details.Contracts;

public sealed record DetailsContract(IReadOnlyList<ConnectorDetailGroup> Groups);

public sealed record ConnectorDetailGroup(ConnectorKey ConnectorKey, IReadOnlyList<DetailField> Fields);
