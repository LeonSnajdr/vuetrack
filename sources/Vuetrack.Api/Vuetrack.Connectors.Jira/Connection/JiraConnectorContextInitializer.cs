using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Jira.Connection;

[InjectAs(typeof(IConnectorContextInitializer))]
public class JiraConnectorContextInitializer(IJiraConnectionContextFactory contextFactory) : IConnectorContextInitializer
{
    private IJiraConnectionContextFactory ContextFactory { get; } = contextFactory;

    public string ConnectorKey => JiraConnector.Key;

    public async Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await ContextFactory.CreateAsync(userId, cancellationToken);
        return connection is not null;
    }
}
