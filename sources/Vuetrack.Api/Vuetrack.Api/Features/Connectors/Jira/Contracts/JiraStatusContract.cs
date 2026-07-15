namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

// Connected: a stored connection exists. Healthy: the stored credentials still authenticate against
// Jira right now (false when the token expired/was revoked and the user must reconnect).
public sealed record JiraStatusContract(bool Connected, bool Healthy, string? SiteUrl);
