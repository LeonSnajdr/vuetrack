using Vuetrack.Backends.Abstractions;
using Vuetrack.OAuth.Contractrs;

namespace Vuetrack.Api.Features.Backends;

public interface IBackendConnectionService
{
    BackendKey Key { get; }

    Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken);
}
