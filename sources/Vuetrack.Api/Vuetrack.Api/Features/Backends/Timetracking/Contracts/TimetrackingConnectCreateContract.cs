namespace Vuetrack.Api.Features.Backends.Timetracking.Contracts;

public sealed record TimetrackingConnectCreateContract
{
    public string Code { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string RedirectUri { get; init; } = string.Empty;
}
