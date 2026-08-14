using ErrorOr;

namespace Vuetrack.Api.Features.Integrations.Abstractions;

public interface IIntegration
{
    IntegrationKey Key { get; }

    Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken);
}
