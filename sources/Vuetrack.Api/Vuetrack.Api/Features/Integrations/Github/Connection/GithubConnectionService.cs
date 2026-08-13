using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.OAuth;

namespace Vuetrack.Api.Features.Integrations.Github.Connection;

[InjectAs(typeof(IIntegrationConnectionService))]
public class GithubConnectionService(
    IGithubOAuthApiClient oauthClient,
    IOptions<GithubOptions> oauthOptions,
    IConnectionRepository repository,
    IOAuthTransactionRepository transactionRepository,
    IGithubSessionFactory sessionFactory,
    IConnectionSecretProtector secretProtector,
    IIntegrationRegistry registry,
    ILogger<GithubConnectionService> logger)
    : IntegrationConnectionServiceBase(oauthClient, oauthOptions, repository, transactionRepository, sessionFactory, secretProtector, registry, logger)
{
    private IGithubOAuthApiClient GithubOAuthClient { get; } = oauthClient;

    public override IntegrationKey Key => IntegrationKey.Github;

    protected override async Task<ErrorOr<Dictionary<string, string>>> EstablishAsync(OAuthTokenResponse token, CancellationToken cancellationToken)
    {
        var user = await GithubOAuthClient.GetAuthenticatedUserAsync(token.AccessToken, cancellationToken);

        if (string.IsNullOrEmpty(user.Login))
        {
            return Error.Validation(description: "GitHub did not return a user for these credentials.");
        }

        return new Dictionary<string, string>
        {
            [GithubConnectionAttributes.Login] = user.Login,
        };
    }
}
