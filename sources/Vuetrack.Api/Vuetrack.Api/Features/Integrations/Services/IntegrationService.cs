using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Features.Integrations.Services;

namespace Vuetrack.Api.Features.Integrations.Services;

[Inject]
public class IntegrationService(IEnumerable<IIntegrationConnectionService> connectionServices) : IIntegrationService
{
    private IReadOnlyList<IIntegrationConnectionService> ConnectionServices { get; } = connectionServices.ToList();

    public IIntegrationConnectionService? ResolveConnectionService(IntegrationKey key)
    {
        var connectionService = ConnectionServices.FirstOrDefault(service => service.Key == key);

        return connectionService;
    }

    public async Task<IReadOnlyList<IntegrationContract>> ListAsync(string userId, CancellationToken cancellationToken)
    {
        var tasks = ConnectionServices.Select(service => BuildContractAsync(service, userId, cancellationToken));
        var integrations = await Task.WhenAll(tasks);

        return integrations;
    }

    private static async Task<IntegrationContract> BuildContractAsync(IIntegrationConnectionService connectionService, string userId, CancellationToken cancellationToken)
    {
        var status = await connectionService.GetStatusAsync(userId, cancellationToken);

        return new IntegrationContract(connectionService.Key, status.Connected, status.Healthy);
    }
}

public interface IIntegrationService
{
    IIntegrationConnectionService? ResolveConnectionService(IntegrationKey key);

    Task<IReadOnlyList<IntegrationContract>> ListAsync(string userId, CancellationToken cancellationToken);
}
