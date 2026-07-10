using Samhammer.Options.Abstractions;

namespace Vuetrack.Connectors.Jira;

[Option]
public class JiraOptions
{
    public string IdentityBaseUrl { get; init; } = "https://auth.atlassian.com";

    public string ApiBaseUrl { get; init; } = "https://api.atlassian.com";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string Scopes { get; init; } = "read:jira-work read:jira-user offline_access";

    public int PageSize { get; init; } = 50;

    public int MaxPages { get; init; } = 20;
}
