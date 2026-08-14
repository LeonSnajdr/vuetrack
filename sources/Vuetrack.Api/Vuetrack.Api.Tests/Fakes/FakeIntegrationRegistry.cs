using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeIntegrationRegistry : IIntegrationRegistry
{
    private readonly List<IIntegration> integrations = [];

    private readonly HashSet<IntegrationKey> connectedKeys = [];

    public void Add(IIntegration integration) => integrations.Add(integration);

    public void AddConnected(IIntegration integration)
    {
        integrations.Add(integration);
        connectedKeys.Add(integration.Key);
    }

    public T? Resolve<T>(IntegrationKey key)
        where T : class, IIntegration
    {
        var integration = integrations.OfType<T>().FirstOrDefault(candidate => candidate.Key == key);

        return integration;
    }

    public IReadOnlyList<T> ResolveAll<T>()
        where T : class, IIntegration
    {
        var found = integrations.OfType<T>().ToList();

        return found;
    }

    public Task<IReadOnlyList<T>> ResolveAllConnectedAsync<T>(string userId, CancellationToken cancellationToken)
        where T : class, IIntegration
    {
        IReadOnlyList<T> found = integrations.OfType<T>().Where(integration => connectedKeys.Contains(integration.Key)).ToList();

        return Task.FromResult(found);
    }
}
