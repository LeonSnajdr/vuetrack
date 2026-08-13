using ErrorOr;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Features.Integrations.Abstractions;

public interface IConnector : IIntegration
{
    Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(string userId, DateRange range, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(string userId, DetailQuery query, CancellationToken cancellationToken);
}
