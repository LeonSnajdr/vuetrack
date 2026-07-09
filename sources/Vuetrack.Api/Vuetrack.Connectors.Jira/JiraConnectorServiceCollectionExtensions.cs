using Microsoft.Extensions.DependencyInjection;
using Vuetrack.Connectors.Jira.ApiClients;

namespace Vuetrack.Connectors.Jira;

public static class JiraConnectorServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="JiraApiClient"/> as a typed <see cref="HttpClient"/> whose pipeline includes
    /// <see cref="JiraAuthHandler"/>, so the ambient access token is attached automatically. The rest of
    /// the connector's services are wired by attribute-based DI; only this typed client + delegating
    /// handler need explicit registration.
    /// </summary>
    public static IServiceCollection AddJiraConnectorHttpClients(this IServiceCollection services)
    {
        services.AddTransient<JiraAuthHandler>();
        services.AddHttpClient<IJiraApiClient, JiraApiClient>()
            .AddHttpMessageHandler<JiraAuthHandler>();
        return services;
    }
}
