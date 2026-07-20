using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira;
using Vuetrack.Connectors.Jira.Connection;
using Vuetrack.Connectors.Jira.OAuth;
using Vuetrack.OAuth;
using Vuetrack.OAuth.Contractrs;

namespace Vuetrack.Api.Features.Connectors.Jira.Services;

[Inject(Target.All)]
public class JiraConnectionService(
    IConnectorRegistry registry,
    IConnectorResolver resolver,
    IJiraOAuthApiClient oauthClient,
    IJiraConnectionRepository repository,
    IJiraConnectorSecretProtector secretProtector,
    IJiraConnectionAccessor accessor,
    IJiraConnectionContextFactory contextFactory,
    ILogger<JiraConnectionService> logger)
    : OAuthConnectionServiceBase<JiraConnectionModel>(oauthClient, repository, contextFactory, secretProtector, logger), IJiraConnectionService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IConnectorResolver Resolver { get; } = resolver;

    private IJiraOAuthApiClient JiraOAuthClient { get; } = oauthClient;

    private IJiraConnectionRepository Repository { get; } = repository;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    protected override string ProviderName => nameof(ConnectorKey.Jira);

    public ConnectorKey Key => ConnectorKey.Jira;

    public async Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await JiraOAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            var resources = await JiraOAuthClient.GetAccessibleResourcesAsync(token.AccessToken, cancellationToken);
            var site = resources.FirstOrDefault();
            if (site is null)
            {
                return Error.Conflict(code: "Jira.NoSite", description: "No accessible Jira site for this account.");
            }

            Accessor.Current = new JiraConnectionContainer
            {
                UserId = userId,
                AccessToken = token.AccessToken,
                CloudId = site.CloudId,
                SiteUrl = site.Url,
            };

            var connector = Registry.Resolve(JiraConnector.Key) ?? throw new InvalidOperationException("Jira connector is not registered.");

            var validation = await connector.ValidateAsync(cancellationToken);
            if (validation.IsError)
            {
                return validation.Errors;
            }

            await PersistConnection(userId, site, token);
            return new OAuthConnectContract(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Jira connect failed");
            return Error.Unexpected();
        }
    }

    protected override async Task<bool> CheckHealthAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(JiraConnector.Key, userId, cancellationToken);
            if (resolved.IsError)
            {
                return false;
            }

            var validation = await resolved.Value.ValidateAsync(cancellationToken);
            return !validation.IsError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Jira status health check failed for user {UserId}", userId);
            return false;
        }
    }

    private async Task PersistConnection(string userId, JiraAccessibleResourceResponse site, OAuthTokenResponse token)
    {
        var encryptedRefreshToken = ProtectRefreshToken(token);
        if (encryptedRefreshToken is null)
        {
            return;
        }

        await Repository.UpsertConnectionAsync(userId, site.Url, site.CloudId, "oauth2-3lo", encryptedRefreshToken);
    }
}

public interface IJiraConnectionService : IConnectorConnectionService
{
    OAuthAuthorizeContract BuildAuthorization(string redirectUri);

    Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId);
}
