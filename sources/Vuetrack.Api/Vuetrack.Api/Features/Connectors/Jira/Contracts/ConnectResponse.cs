namespace Vuetrack.Api.Features.Connectors.Jira.Contracts;

public sealed record ConnectResponse(bool Valid, IReadOnlyList<string> Errors);
