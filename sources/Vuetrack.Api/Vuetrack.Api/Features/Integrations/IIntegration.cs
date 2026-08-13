using ErrorOr;

namespace Vuetrack.Api.Features.Integrations;

public interface IIntegration
{
    IntegrationKey Key { get; }

    Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken);
}
