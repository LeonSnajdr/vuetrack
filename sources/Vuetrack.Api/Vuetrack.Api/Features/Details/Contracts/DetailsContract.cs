using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Details.Contracts;

public sealed record DetailsContract(IReadOnlyList<IntegrationDetailGroup> Groups);

public sealed record IntegrationDetailGroup(IntegrationKey Key, IReadOnlyList<DetailField> Fields);
