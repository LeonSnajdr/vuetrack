using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira;
using Vuetrack.Connectors.Jira.ApiClients;
using Vuetrack.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Jira.Exceptions;
using Vuetrack.Connectors.Jira.Models;
using Vuetrack.Connectors.Jira.Repositories;
using Vuetrack.Connectors.Jira.Services;

namespace Vuetrack.Api.Features.Connectors.Jira.Services;

[Inject]
public class JiraConnectionService(
    IConnectorRegistry registry,
    IJiraOAuthApiClient oauthClient,
    IJiraConnectionRepository repository,
    ISecretProtector secretProtector,
    IJiraConnectionAccessor accessor,
    ILogger<JiraConnectionService> logger) : IJiraConnectionService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IJiraOAuthApiClient OAuthClient { get; } = oauthClient;

    private IJiraConnectionRepository Repository { get; } = repository;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    private ILogger<JiraConnectionService> Logger { get; } = logger;

    public JiraAuthorizeResponse BuildAuthorization(string redirectUri)
    {
        var state = Guid.NewGuid().ToString("N");
        var url = OAuthClient.BuildAuthorizationUrl(state, redirectUri);
        return new JiraAuthorizeResponse(url, state);
    }

    public async Task<JiraStatusResponse> GetStatusAsync(string userId)
    {
        var connection = await Repository.GetByUserId(userId);
        return new JiraStatusResponse(connection is { Enabled: true }, connection?.SiteUrl);
    }

    public async Task<ConnectResponse> ConnectAsync(string userId, JiraConnectRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
            var resources = await OAuthClient.GetAccessibleResourcesAsync(token.AccessToken, cancellationToken);
            var site = resources.FirstOrDefault();
            if (site is null)
            {
                return new ConnectResponse(false, ["No accessible Jira site for this account."]);
            }

            Accessor.Current = new JiraConnection
            {
                UserId = userId,
                AccessToken = token.AccessToken,
                CloudId = site.CloudId,
                SiteUrl = site.Url,
            };

            var connector = Registry.Resolve(JiraConnector.Key)
                ?? throw new InvalidOperationException("Jira connector is not registered.");

            var outcome = await connector.ValidateAsync(cancellationToken);
            if (outcome is Invalid invalid)
            {
                return new ConnectResponse(false, invalid.Errors);
            }

            await PersistConnection(userId, site, token);
            return new ConnectResponse(true, []);
        }
        catch (JiraApiException ex)
        {
            Logger.LogWarning("Jira connect failed: {Kind}", ex.Kind);
            return new ConnectResponse(false, [ex.Message]);
        }
    }

    private async Task PersistConnection(string userId, JiraAccessibleResourceResponse site, JiraTokenResponse token)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("Jira token response had no refresh token; connection not persisted.");
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
    JiraAuthorizeResponse BuildAuthorization(string redirectUri);

    Task<JiraStatusResponse> GetStatusAsync(string userId);

    Task<ConnectResponse> ConnectAsync(string userId, JiraConnectRequest request, CancellationToken cancellationToken);
}
