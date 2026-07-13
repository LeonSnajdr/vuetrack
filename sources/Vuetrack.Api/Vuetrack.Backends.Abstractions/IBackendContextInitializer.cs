namespace Vuetrack.Backends.Abstractions;

public interface IBackendContextInitializer
{
    BackendKey BackendKey { get; }

    Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken);
}
