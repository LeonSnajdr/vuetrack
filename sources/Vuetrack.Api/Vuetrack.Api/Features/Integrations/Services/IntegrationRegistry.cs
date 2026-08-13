using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Services;

[Inject]
public class IntegrationRegistry(IEnumerable<IConnector> connectors, IEnumerable<IBackend> timeEntryStores) : IIntegrationRegistry
{
    private IReadOnlyList<IIntegration> Integrations { get; } = [.. connectors.Cast<IIntegration>().Concat(timeEntryStores).Distinct()];

    public T? Resolve<T>(IntegrationKey key)
        where T : class, IIntegration
    {
        var integration = Integrations.OfType<T>().FirstOrDefault(candidate => candidate.Key == key);

        return integration;
    }

    public IReadOnlyList<T> ResolveAll<T>()
        where T : class, IIntegration
    {
        var integrations = Integrations.OfType<T>().ToList();

        return integrations;
    }
}
