using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Jira.Connection;

public class JiraSessionFactory(
    HttpClient httpClient,
    IOptions<JiraOptions> options,
    ILogger<JiraSession> sessionLogger,
    IConnectionRepository repository,
    IJiraOAuthApiClient oauthClient,
    IConnectionSecretProtector secretProtector,
    IFusionCache cache)
    : ConnectionSessionFactory<IJiraSession>(cache, repository, oauthClient, secretProtector), IJiraSessionFactory
{
    private HttpClient HttpClient { get; } = httpClient;

    private IOptions<JiraOptions> Options { get; } = options;

    private ILogger<JiraSession> SessionLogger { get; } = sessionLogger;

    protected override IntegrationKey Key => IntegrationKey.Jira;

    protected override IJiraSession CreateSession(ConnectionCredentials credentials)
    {
        var cloudId = credentials.Attributes.GetValueOrDefault(JiraConnectionAttributes.CloudId, string.Empty);
        var siteUrl = credentials.Attributes.GetValueOrDefault(JiraConnectionAttributes.SiteUrl, string.Empty);

        return new JiraSession(HttpClient, Options, SessionLogger, credentials.AccessToken, cloudId, siteUrl);
    }
}

public interface IJiraSessionFactory : IConnectionSessionFactory<IJiraSession>;
