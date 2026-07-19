using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Github.Connection;

[InjectAs(typeof(IConnectorContextInitializer))]
public class GithubConnectorContextInitializer(IGithubConnectionContextFactory contextFactory) : IConnectorContextInitializer
{
    private IGithubConnectionContextFactory ContextFactory { get; } = contextFactory;

    public ConnectorKey ConnectorKey => GithubConnector.Key;

    public async Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await ContextFactory.CreateAsync(userId, cancellationToken);
        return connection is not null;
    }
}
