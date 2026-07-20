using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Github.Activity;

// Connector-specific facts behind a GitHub ActivitySignal. Opaque to the engine; the model reasons over
// its serialized shape and connector-aware code downcasts to it.
public sealed record GithubSignalDetail : IConnectorSignalDetail
{
    public string? RepoName { get; init; }

    public string? Message { get; init; }
}
