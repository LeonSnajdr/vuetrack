using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;

namespace Vuetrack.Backends.Timetracking.Connection;

[InjectAs(typeof(IBackendContextInitializer))]
public class TimetrackingBackendContextInitializer(ITimetrackingConnectionContextFactory contextFactory) : IBackendContextInitializer
{
    private ITimetrackingConnectionContextFactory ContextFactory { get; } = contextFactory;

    public BackendKey BackendKey => TimetrackingBackend.Key;

    public async Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await ContextFactory.CreateAsync(userId, cancellationToken);
        return connection is not null;
    }
}
