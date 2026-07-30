using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Details;

[Inject]
public class DetailService(IConnectorResolver resolver, ILogger<DetailService> logger) : IDetailService
{
    private IConnectorResolver Resolver { get; } = resolver;

    private ILogger<DetailService> Logger { get; } = logger;

    public async Task<DetailsContract> GetAsync(DetailQuery query, string userId, CancellationToken cancellationToken)
    {
        var connectors = await Resolver.ResolveAllConnectedAsync(userId, cancellationToken);

        var detailTasks = connectors.Select(async connector =>
        {
            var fields = await GetFromConnectorAsync(connector, query, userId, cancellationToken);
            return (connector.Descriptor.Key, Fields: fields);
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

    private async Task<IReadOnlyList<DetailField>> GetFromConnectorAsync(IConnector connector, DetailQuery query, string userId, CancellationToken cancellationToken)
    {
        var key = connector.Descriptor.Key;

        try
        {
            var details = await connector.GetDetailsAsync(query, cancellationToken);
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
