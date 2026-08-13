using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Jira.OAuth;

namespace Vuetrack.Api.Features.Integrations.Jira.Connection;

[InjectAs(typeof(IIntegrationConnectionService))]
public class JiraConnectionService(
    IJiraOAuthApiClient oauthClient,
    IOptions<JiraOptions> oauthOptions,
    IConnectionRepository repository,
    IOAuthTransactionRepository transactionRepository,
    IJiraSessionFactory sessionFactory,
    IConnectionSecretProtector secretProtector,
    IIntegrationRegistry registry,
    ILogger<JiraConnectionService> logger)
    : IntegrationConnectionServiceBase(oauthClient, oauthOptions, repository, transactionRepository, sessionFactory, secretProtector, registry, logger)
{
    private IJiraOAuthApiClient JiraOAuthClient { get; } = oauthClient;

    public override IntegrationKey Key => IntegrationKey.Jira;

    protected override async Task<ErrorOr<Dictionary<string, string>>> EstablishAsync(OAuthTokenResponse token, CancellationToken cancellationToken)
    {
        var resources = await JiraOAuthClient.GetAccessibleResourcesAsync(token.AccessToken, cancellationToken);

        var site = resources.FirstOrDefault();
        if (site is null)
        {
            return Error.Conflict(code: "Jira.NoSite", description: "No accessible Jira site for this account.");
        }

        return new Dictionary<string, string>
        {
            [JiraConnectionAttributes.SiteUrl] = site.Url,
            [JiraConnectionAttributes.CloudId] = site.CloudId,
        };
    }
}
