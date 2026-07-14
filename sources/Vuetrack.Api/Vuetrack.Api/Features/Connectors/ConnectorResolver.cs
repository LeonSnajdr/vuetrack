using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Framework.Errors;

namespace Vuetrack.Api.Features.Connectors;

[Inject]
public class ConnectorResolver(IConnectorRegistry registry, IEnumerable<IConnectorContextInitializer> contextInitializers) : IConnectorResolver
{
    private IConnectorRegistry Registry { get; } = registry;

    private IReadOnlyList<IConnectorContextInitializer> ContextInitializers { get; } = contextInitializers.ToList();

    public async Task<ErrorOr<IConnector>> ResolveConnectedAsync(ConnectorKey key, string userId, CancellationToken cancellationToken)
    {
        var initializer = ContextInitializers.FirstOrDefault(i => i.ConnectorKey == key);
        if (initializer is null)
        {
            return ConnectorError.NotConnected;
        }

        var initialized = await initializer.TryInitializeAsync(userId, cancellationToken);
        if (!initialized)
        {
            return ConnectorError.NotConnected;
        }

        var connector = Registry.Resolve(key);
        if (connector is null)
        {
            return ConnectorError.NotConnected;
        }

        return connector.ToErrorOr();
    }
}

public interface IConnectorResolver
{
    Task<ErrorOr<IConnector>> ResolveConnectedAsync(ConnectorKey key, string userId, CancellationToken cancellationToken);
}
