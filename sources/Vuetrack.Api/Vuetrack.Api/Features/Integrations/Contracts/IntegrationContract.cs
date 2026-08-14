using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Contracts;

public sealed record IntegrationContract(IntegrationKey Key, bool Connected, bool Healthy);
