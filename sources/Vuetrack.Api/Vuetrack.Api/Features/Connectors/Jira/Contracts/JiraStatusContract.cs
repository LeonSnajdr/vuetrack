namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record JiraStatusContract(bool Connected, string? SiteUrl);
