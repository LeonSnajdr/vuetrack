namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record JiraConnectRequest
{
    public string Code { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string RedirectUri { get; init; } = string.Empty;
}
