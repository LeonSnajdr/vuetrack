using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Details.Services;

[Inject]
public class DetailService(IIntegrationRegistry registry, ILogger<DetailService> logger) : IDetailService
{
    private IIntegrationRegistry Registry { get; } = registry;

    private ILogger<DetailService> Logger { get; } = logger;

    public async Task<ErrorOr<DetailsContract>> GetAsync(DetailQuery query, string userId, CancellationToken cancellationToken)
    {
        var connectors = Registry.ResolveAll<IConnector>();

        var detailTasks = connectors.Select(async connector =>
        {
            var fields = await GetFromConnectorAsync(connector, query, userId, cancellationToken);
            return (connector.Key, Fields: fields);
        });

        var results = await Task.WhenAll(detailTasks);

        var groups = new List<IntegrationDetailGroup>();
        foreach (var result in results)
        {
            if (result.Fields.Count == 0)
            {
                continue;
            }

            var group = new IntegrationDetailGroup(result.Key, result.Fields);
            groups.Add(group);
        }

        return new DetailsContract(groups);
    }

    private async Task<IReadOnlyList<DetailField>> GetFromConnectorAsync(IConnector connector, DetailQuery query, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var details = await connector.GetDetailsAsync(userId, query, cancellationToken);
            if (details.IsError)
            {
                Logger.LogWarning("Integration {Integration} failed to fetch details for user {UserId}: {Error}", connector.Key, userId, details.FirstError.Description);
                return [];
            }

            return details.Value;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Integration {Integration} threw while fetching details for user {UserId}", connector.Key, userId);
            return [];
        }
    }
}

public interface IDetailService
{
    Task<ErrorOr<DetailsContract>> GetAsync(DetailQuery query, string userId, CancellationToken cancellationToken);
}
