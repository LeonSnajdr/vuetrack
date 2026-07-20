using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Details;

[Inject]
public class DetailService(IConnectorRegistry registry, IConnectorResolver resolver, ILogger<DetailService> logger) : IDetailService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IConnectorResolver Resolver { get; } = resolver;

    private ILogger<DetailService> Logger { get; } = logger;

    public async Task<DetailsContract> GetAsync(DetailQuery query, string userId, CancellationToken cancellationToken)
    {
        var detailTasks = Registry.Descriptors.Select(async descriptor =>
        {
            var fields = await GetFromConnectorAsync(descriptor.Key, query, userId, cancellationToken);
            return (descriptor.Key, Fields: fields);
        });

        var results = await Task.WhenAll(detailTasks);

        var groups = new List<ConnectorDetailGroup>();
        foreach (var result in results)
        {
            if (result.Fields.Count == 0)
            {
                continue;
            }

            var group = new ConnectorDetailGroup(result.Key, result.Fields);
            groups.Add(group);
        }

        return new DetailsContract(groups);
    }

    private async Task<IReadOnlyList<DetailField>> GetFromConnectorAsync(ConnectorKey key, DetailQuery query, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(key, userId, cancellationToken);
            if (resolved.IsError)
            {
                Logger.LogInformation("Connector {ConnectorKey} unavailable for user {UserId}: {Error}", key, userId, resolved.FirstError.Description);
                return [];
            }

            var details = await resolved.Value.GetDetailsAsync(query, cancellationToken);
            if (details.IsError)
            {
                Logger.LogWarning("Connector {ConnectorKey} failed to fetch details for user {UserId}: {Error}", key, userId, details.FirstError.Description);
                return [];
            }

            return details.Value;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Connector {ConnectorKey} threw while fetching details for user {UserId}", key, userId);
            return [];
        }
    }
}

public interface IDetailService
{
    Task<DetailsContract> GetAsync(DetailQuery query, string userId, CancellationToken cancellationToken);
}
