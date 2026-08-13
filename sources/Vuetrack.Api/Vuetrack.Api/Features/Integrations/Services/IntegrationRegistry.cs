using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;

namespace Vuetrack.Api.Features.Integrations.Services;

[Inject]
public class IntegrationRegistry(IEnumerable<IConnector> connectors, IEnumerable<IBackend> timeEntryStores, IConnectionRepository connections) : IIntegrationRegistry
{
    private IReadOnlyList<IIntegration> Integrations { get; } = [.. connectors.Cast<IIntegration>().Concat(timeEntryStores).Distinct()];

    private IConnectionRepository Connections { get; } = connections;

    public T? Resolve<T>(IntegrationKey key) where T : class, IIntegration
    {
        var integration = Integrations.OfType<T>().FirstOrDefault(candidate => candidate.Key == key);

        return integration;
    }

    public IReadOnlyList<T> ResolveAll<T>() where T : class, IIntegration
    {
        var integrations = Integrations.OfType<T>().ToList();

        return integrations;
    }

    public async Task<IReadOnlyList<T>> ResolveAllConnectedAsync<T>(string userId, CancellationToken cancellationToken) where T : class, IIntegration
    {
        var connectedKeys = await Connections.GetConnectedKeysAsync(userId, cancellationToken);
        var integrations = Integrations.OfType<T>().Where(integration => connectedKeys.Contains(integration.Key)).ToList();

        return integrations;
    }
}
