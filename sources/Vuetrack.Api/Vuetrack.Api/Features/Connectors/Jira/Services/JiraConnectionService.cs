using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira;
using Vuetrack.Connectors.Jira.Connection;
using Vuetrack.Connectors.Jira.OAuth;
using Vuetrack.OAuth;

namespace Vuetrack.Api.Features.Connectors.Jira.Services;

[Inject]
public class JiraConnectionService(IConnectorRegistry registry, IJiraOAuthApiClient oauthClient, IJiraConnectionRepository repository, ISecretProtector secretProtector, IJiraConnectionAccessor accessor, IJiraConnectionContextFactory contextFactory, ILogger<JiraConnectionService> logger) : IJiraConnectionService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IJiraOAuthApiClient OAuthClient { get; } = oauthClient;

    private IJiraConnectionRepository Repository { get; } = repository;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    private IJiraConnectionContextFactory ContextFactory { get; } = contextFactory;

    private ILogger<JiraConnectionService> Logger { get; } = logger;

    public JiraAuthorizeContract BuildAuthorization(string redirectUri)
    {
        var state = Guid.NewGuid().ToString("N");
        var url = OAuthClient.BuildAuthorizationUrl(state, redirectUri);
        return new JiraAuthorizeContract(url, state);
    }

    public async Task<JiraStatusContract> GetStatusAsync(string userId)
    {
        var connection = await Repository.GetByUserId(userId);
        return new JiraStatusContract(connection is { Enabled: true }, connection?.SiteUrl);
    }

    public async Task<ErrorOr<JiraConnectContract>> ConnectAsync(string userId, JiraConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            var resources = await OAuthClient.GetAccessibleResourcesAsync(token.AccessToken, cancellationToken);
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
            return new JiraConnectContract(site.Url);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Jira connect failed");
            return Error.Unexpected();
        }
    }

    public async Task DisconnectAsync(string userId)
    {
        var connection = await Repository.GetByUserId(userId);
        if (connection is not null)
        {
            await Repository.Delete(connection);
        }

        ContextFactory.Evict(userId);
    }

    private async Task PersistConnection(string userId, JiraAccessibleResourceResponse site, OAuthTokenResponse token)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("Jira token response had no refresh token; connection not persisted");
            return;
        }

        var encryptedRefreshToken = SecretProtector.Protect(token.RefreshToken);

        await Repository.UpsertConnectionAsync(userId, site.Url, site.CloudId, "oauth2-3lo", encryptedRefreshToken);
    }
}

public interface IJiraConnectionService
{
    JiraAuthorizeContract BuildAuthorization(string redirectUri);

    Task<JiraStatusContract> GetStatusAsync(string userId);

    Task<ErrorOr<JiraConnectContract>> ConnectAsync(string userId, JiraConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId);
}
