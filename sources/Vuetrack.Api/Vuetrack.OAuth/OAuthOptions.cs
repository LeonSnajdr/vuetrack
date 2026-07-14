namespace Vuetrack.OAuth;

public abstract class OAuthOptions
{
    public required string AuthorizeEndpoint { get; init; }

    public required string TokenEndpoint { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }

    public required string Scopes { get; init; }
}
