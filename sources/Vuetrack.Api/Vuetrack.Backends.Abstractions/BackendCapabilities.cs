namespace Vuetrack.Backends.Abstractions;

[Flags]
public enum BackendCapabilities
{
    None = 0,

    TimeEntries = 1 << 0,

    Projects = 1 << 1,

    OAuth = 1 << 2,
}
