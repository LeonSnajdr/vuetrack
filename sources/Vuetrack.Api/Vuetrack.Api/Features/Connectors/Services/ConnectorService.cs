using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Connectors.Services;

[Inject]
public class ConnectorService(IConnectorRegistry registry, IEnumerable<IConnectorConnectionService> connectionServices) : IConnectorService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IReadOnlyList<IConnectorConnectionService> ConnectionServices { get; } = connectionServices.ToList();

    public async Task<IReadOnlyList<ConnectorContract>> GetConnectorsAsync(string userId, CancellationToken cancellationToken)
    {
        var tasks = Registry.Descriptors.Select(descriptor => BuildContractAsync(descriptor, userId, cancellationToken));
        var connectors = await Task.WhenAll(tasks);

        return connectors;
    }

    private async Task<ConnectorContract> BuildContractAsync(ConnectorDescriptor descriptor, string userId, CancellationToken cancellationToken)
    {
        var connectionService = ConnectionServices.FirstOrDefault(service => service.Key == descriptor.Key);

        if (connectionService is null)
        {
            return new ConnectorContract(descriptor.Key, descriptor.Capabilities, false, false);
        }

        var status = await connectionService.GetStatusAsync(userId, cancellationToken);

        return new ConnectorContract(descriptor.Key, descriptor.Capabilities, status.Connected, status.Healthy);
    }
}

public interface IConnectorService
{
    Task<IReadOnlyList<ConnectorContract>> GetConnectorsAsync(string userId, CancellationToken cancellationToken);
}
