namespace Vuetrack.Api.Features.Connectors.Jira;

public abstract record JiraConnectResult;

public sealed record JiraConnectSuccess(string SiteUrl) : JiraConnectResult;

public sealed record JiraConnectNoSite : JiraConnectResult;

public sealed record JiraConnectValidationFailed(IReadOnlyList<string> Errors) : JiraConnectResult;

public sealed record JiraConnectAuthFailed(string Reason) : JiraConnectResult;

public sealed record JiraConnectRateLimited(TimeSpan RetryAfter) : JiraConnectResult;

public sealed record JiraConnectError(string Message) : JiraConnectResult;
