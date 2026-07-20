using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;

namespace Vuetrack.Api.Features.Backends.Services;

[Inject]
public class BackendService(IBackendRegistry registry, IEnumerable<IBackendConnectionService> connectionServices) : IBackendService
{
    private IBackendRegistry Registry { get; } = registry;

    private IReadOnlyList<IBackendConnectionService> ConnectionServices { get; } = connectionServices.ToList();

    public async Task<IReadOnlyList<BackendContract>> GetBackendsAsync(string userId, CancellationToken cancellationToken)
    {
        var tasks = Registry.Descriptors.Select(descriptor => BuildContractAsync(descriptor, userId, cancellationToken));
        var backends = await Task.WhenAll(tasks);

        return backends;
    }

    private async Task<BackendContract> BuildContractAsync(BackendDescriptor descriptor, string userId, CancellationToken cancellationToken)
    {
        var connectionService = ConnectionServices.FirstOrDefault(service => service.Key == descriptor.Key);

        if (connectionService is null)
        {
            return new BackendContract(descriptor.Key, descriptor.Capabilities, false, false);
        }

        var status = await connectionService.GetStatusAsync(userId, cancellationToken);

        return new BackendContract(descriptor.Key, descriptor.Capabilities, status.Connected, status.Healthy);
    }
}

public interface IBackendService
{
    Task<IReadOnlyList<BackendContract>> GetBackendsAsync(string userId, CancellationToken cancellationToken);
}
