namespace Vuetrack.OAuth.Contractrs;

public sealed record OAuthConnectCreateContract
{
    public string Code { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string RedirectUri { get; init; } = string.Empty;
}
