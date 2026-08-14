namespace Vuetrack.Api.Features.Integrations.Contracts;

public sealed record OAuthAuthorizeContract(string AuthorizationUrl, string State);
