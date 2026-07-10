namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record JiraAuthorizeContract(string AuthorizationUrl, string State);
