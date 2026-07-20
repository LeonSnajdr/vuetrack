using Vuetrack.Connectors.Abstractions;
using Vuetrack.OAuth.Contractrs;

namespace Vuetrack.Api.Features.Connectors;

public interface IConnectorConnectionService
{
    ConnectorKey Key { get; }

    Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken);
}
