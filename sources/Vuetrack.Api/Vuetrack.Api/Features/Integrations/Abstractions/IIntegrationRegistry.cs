namespace Vuetrack.Api.Features.Integrations.Abstractions;

public interface IIntegrationRegistry
{
    T? Resolve<T>(IntegrationKey key) where T : class, IIntegration;

    IReadOnlyList<T> ResolveAll<T>() where T : class, IIntegration;

    Task<IReadOnlyList<T>> ResolveAllConnectedAsync<T>(string userId, CancellationToken cancellationToken) where T : class, IIntegration;
}
