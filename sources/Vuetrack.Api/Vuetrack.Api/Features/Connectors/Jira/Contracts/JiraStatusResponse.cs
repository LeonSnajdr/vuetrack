namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record JiraStatusResponse(bool Connected, string? SiteUrl);
