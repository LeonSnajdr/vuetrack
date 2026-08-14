using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.Api;
using Vuetrack.Api.Features.Integrations.Github.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Github.Connection;

public class GithubSessionFactory(
    HttpClient httpClient,
    IOptions<GithubOptions> options,
    ILogger<GithubSession> sessionLogger,
    IConnectionRepository repository,
    IGithubOAuthApiClient oauthClient,
    IConnectionSecretProtector secretProtector,
    IFusionCache cache)
    : ConnectionSessionFactory<IGithubSession>(cache, repository, oauthClient, secretProtector), IGithubSessionFactory
{
    private HttpClient HttpClient { get; } = httpClient;

    private IOptions<GithubOptions> Options { get; } = options;

    private ILogger<GithubSession> SessionLogger { get; } = sessionLogger;

    protected override IntegrationKey Key => IntegrationKey.Github;

    protected override IGithubSession CreateSession(ConnectionCredentials credentials)
    {
        var login = credentials.Attributes.GetValueOrDefault(GithubConnectionAttributes.Login, string.Empty);

        return new GithubSession(HttpClient, Options, SessionLogger, credentials.AccessToken, login);
    }
}

public interface IGithubSessionFactory : IConnectionSessionFactory<IGithubSession>;
