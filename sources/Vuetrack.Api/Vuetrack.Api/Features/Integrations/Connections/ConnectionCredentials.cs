namespace Vuetrack.Api.Features.Integrations.Connections;

/// <summary>
/// What gets cached: a live access token plus the connection facts persisted at connect time.
/// Sessions are built from this per operation and are never cached themselves, because they hold an HttpClient.
/// </summary>
public sealed record ConnectionCredentials
{
    public required string AccessToken { get; init; }

    public required Dictionary<string, string> Attributes { get; init; }
}
