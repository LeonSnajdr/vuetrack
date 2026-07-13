using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;

namespace Vuetrack.Api.Features.Backends;

[Inject]
public class BackendRegistry(IEnumerable<IBackend> backends) : IBackendRegistry
{
    private IReadOnlyList<IBackend> Backends { get; } = backends.ToList();

    public IReadOnlyList<BackendDescriptor> Descriptors => Backends.Select(backend => backend.Descriptor).ToList();

    public IBackend? Resolve(BackendKey key) => Backends.FirstOrDefault(backend => backend.Descriptor.Key == key);
}
