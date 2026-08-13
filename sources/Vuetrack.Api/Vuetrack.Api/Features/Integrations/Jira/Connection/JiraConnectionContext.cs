namespace Vuetrack.Api.Features.Integrations.Jira.Connection;

public sealed record JiraConnectionContext
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string CloudId { get; init; }

    public required string SiteUrl { get; init; }
}

public static class JiraConnectionAttributes
{
    public const string SiteUrl = "siteUrl";

    public const string CloudId = "cloudId";
}
