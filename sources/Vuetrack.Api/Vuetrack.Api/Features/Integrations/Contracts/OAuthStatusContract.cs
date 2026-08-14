namespace Vuetrack.Api.Features.Integrations.Contracts;

public sealed record OAuthStatusContract(bool Connected, bool Healthy);
