using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira;
using Vuetrack.Connectors.Jira.Connection;
using Vuetrack.Connectors.Jira.OAuth;

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

    public async Task<JiraConnectResult> ConnectAsync(string userId, JiraConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            var resources = await OAuthClient.GetAccessibleResourcesAsync(token.AccessToken, cancellationToken);
            var site = resources.FirstOrDefault();
            if (site is null)
            {
                return new JiraConnectNoSite();
            }

            Accessor.Current = new JiraConnectionContainer
            {
                UserId = userId,
                AccessToken = token.AccessToken,
                CloudId = site.CloudId,
                SiteUrl = site.Url,
            };

            var connector = Registry.Resolve(JiraConnector.Key) ?? throw new InvalidOperationException("Jira connector is not registered.");

            var result = await connector.ValidateAsync(cancellationToken);
            if (result is ConnectorValidationInvalid invalid)
            {
                return new JiraConnectValidationFailed(invalid.Errors);
            }

            await PersistConnection(userId, site, token);
            return new JiraConnectSuccess(site.Url);
        }
        catch (JiraApiException ex)
        {
            Logger.LogWarning("Jira connect failed: {Kind}", ex.Kind);
            return ex.Kind switch
            {
                JiraApiErrorKind.Auth => new JiraConnectAuthFailed(ex.Message),
                JiraApiErrorKind.RateLimited => new JiraConnectRateLimited(ex.RetryAfter ?? TimeSpan.FromSeconds(60)),
                _ => new JiraConnectError(ex.Message),
            };
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

    private async Task PersistConnection(string userId, JiraAccessibleResourceResponse site, JiraTokenResponse token)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("Jira token response had no refresh token; connection not persisted");
            return;
        }

        var connection = await Repository.GetByUserId(userId) ?? new JiraConnectionModel
        {
            UserId = userId,
            SiteUrl = site.Url,
            CloudId = site.CloudId,
            AuthMode = "oauth2-3lo",
            EncryptedRefreshToken = string.Empty,
        };

        connection.SiteUrl = site.Url;
        connection.CloudId = site.CloudId;
        connection.AuthMode = "oauth2-3lo";
        connection.EncryptedRefreshToken = SecretProtector.Protect(token.RefreshToken);
        connection.Enabled = true;

        await Repository.Save(connection);
    }
}

public interface IJiraConnectionService
{
    JiraAuthorizeContract BuildAuthorization(string redirectUri);

    Task<JiraStatusContract> GetStatusAsync(string userId);

    Task<JiraConnectResult> ConnectAsync(string userId, JiraConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId);
}
