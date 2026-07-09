namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record JiraAuthorizeResponse(string AuthorizationUrl, string State);
