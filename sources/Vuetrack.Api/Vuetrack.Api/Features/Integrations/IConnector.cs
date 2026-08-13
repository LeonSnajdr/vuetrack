using ErrorOr;
using Vuetrack.Api.Features.Details;

namespace Vuetrack.Api.Features.Integrations;

public interface IConnector : IIntegration
{
    Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(string userId, ActivityFetchContainer container, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(string userId, DetailQuery query, CancellationToken cancellationToken);
}
