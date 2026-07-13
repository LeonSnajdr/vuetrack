using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;
using Vuetrack.Framework.Errors;

namespace Vuetrack.Api.Features.Backends;

/// <summary>
/// Resolves the backend for a user, running its per-user OAuth context initialization first.
/// Returns null when the user has no active connection, so callers can surface a "not connected" state.
/// </summary>
[Inject]
public class BackendResolver(IBackendRegistry registry, IEnumerable<IBackendContextInitializer> contextInitializers) : IBackendResolver
{
    private IBackendRegistry Registry { get; } = registry;

    private IReadOnlyList<IBackendContextInitializer> ContextInitializers { get; } = contextInitializers.ToList();

    public async Task<ErrorOr<IBackend>> ResolveConnectedAsync(BackendKey key, string userId, CancellationToken cancellationToken)
    {
        var initializer = ContextInitializers.FirstOrDefault(i => i.BackendKey == key);
        if (initializer is null)
        {
            return BackendError.NotConnected;
        }

        var initialized = await initializer.TryInitializeAsync(userId, cancellationToken);
        if (!initialized)
        {
            return BackendError.NotConnected;
        }

        var backend = Registry.Resolve(key);
        if (backend is null)
        {
            return BackendError.NotConnected;
        }

        return backend.ToErrorOr();
    }
}

public interface IBackendResolver
{
    Task<ErrorOr<IBackend>> ResolveConnectedAsync(BackendKey key, string userId, CancellationToken cancellationToken);
}
