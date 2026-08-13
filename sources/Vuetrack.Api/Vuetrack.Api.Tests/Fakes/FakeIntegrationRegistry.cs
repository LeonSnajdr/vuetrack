using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeIntegrationRegistry : IIntegrationRegistry
{
    private readonly List<IIntegration> integrations = [];

    public void Add(IIntegration integration) => integrations.Add(integration);

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
}
