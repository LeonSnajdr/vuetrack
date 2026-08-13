namespace Vuetrack.Api.Features.Integrations.Abstractions;

public interface IIntegrationRegistry
{
    T? Resolve<T>(IntegrationKey key)
        where T : class, IIntegration;

    IReadOnlyList<T> ResolveAll<T>()
        where T : class, IIntegration;
}
