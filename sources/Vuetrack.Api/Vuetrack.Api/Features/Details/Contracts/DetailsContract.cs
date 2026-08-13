using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Details.Contracts;

public sealed record DetailsContract(IReadOnlyList<IntegrationDetailGroup> Groups);

public sealed record IntegrationDetailGroup(IntegrationKey Key, IReadOnlyList<DetailField> Fields);
