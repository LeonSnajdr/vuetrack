namespace Vuetrack.Backends.Abstractions;

public interface IBackendRegistry
{
    IReadOnlyList<BackendDescriptor> Descriptors { get; }

    IBackend? Resolve(BackendKey key);
}
