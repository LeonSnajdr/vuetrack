namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record JiraConnectContract(bool Valid, IReadOnlyList<string> Errors);
