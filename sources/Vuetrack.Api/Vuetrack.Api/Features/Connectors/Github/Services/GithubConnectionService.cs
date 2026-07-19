using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Github;
using Vuetrack.Connectors.Github.Connection;
using Vuetrack.Connectors.Github.OAuth;
using Vuetrack.OAuth;
using Vuetrack.OAuth.Contractrs;

namespace Vuetrack.Api.Features.Connectors.Github.Services;

[Inject]
public class GithubConnectionService(
    IConnectorRegistry registry,
    IConnectorResolver resolver,
    IGithubOAuthApiClient oauthClient,
    IGithubConnectionRepository repository,
    IGithubConnectorSecretProtector secretProtector,
    IGithubConnectionAccessor accessor,
    IGithubConnectionContextFactory contextFactory,
    ILogger<GithubConnectionService> logger)
    : OAuthConnectionServiceBase<GithubConnectionModel>(oauthClient, repository, contextFactory, secretProtector, logger), IGithubConnectionService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IConnectorResolver Resolver { get; } = resolver;

    private IGithubOAuthApiClient GithubOAuthClient { get; } = oauthClient;

    private IGithubConnectionRepository Repository { get; } = repository;

    private IGithubConnectionAccessor Accessor { get; } = accessor;

    protected override string ProviderName => nameof(ConnectorKey.Github);

    public async Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await GithubOAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            var user = await GithubOAuthClient.GetAuthenticatedUserAsync(token.AccessToken, cancellationToken);
            if (string.IsNullOrEmpty(user.Login))
            {
                return Error.Conflict(code: "Github.NoUser", description: "No GitHub user for this account.");
            }

            Accessor.Current = new GithubConnectionContainer
            {
                UserId = userId,
                AccessToken = token.AccessToken,
                Login = user.Login,
            };

            var connector = Registry.Resolve(GithubConnector.Key) ?? throw new InvalidOperationException("GitHub connector is not registered.");

            var validation = await connector.ValidateAsync(cancellationToken);
            if (validation.IsError)
            {
                return validation.Errors;
            }

            await PersistConnection(userId, user, token);
            return new OAuthConnectContract(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "GitHub connect failed");
            return Error.Unexpected();
        }
    }

    protected override async Task<bool> CheckHealthAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(GithubConnector.Key, userId, cancellationToken);
            if (resolved.IsError)
            {
                return false;
            }

            var validation = await resolved.Value.ValidateAsync(cancellationToken);
            return !validation.IsError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "GitHub status health check failed for user {UserId}", userId);
            return false;
        }
    }

    private async Task PersistConnection(string userId, GithubUserResponse user, OAuthTokenResponse token)
    {
        var encryptedRefreshToken = ProtectRefreshToken(token);
        if (encryptedRefreshToken is null)
        {
            return;
        }

        await Repository.UpsertConnectionAsync(userId, user.Login, "github-app-user", encryptedRefreshToken);
    }
}

public interface IGithubConnectionService
{
    OAuthAuthorizeContract BuildAuthorization(string redirectUri);

    Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken);

    Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId);
}
