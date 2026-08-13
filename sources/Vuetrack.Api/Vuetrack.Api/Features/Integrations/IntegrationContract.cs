using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Integrations;

public sealed record IntegrationContract(IntegrationKey Key, bool Connected, bool Healthy);
