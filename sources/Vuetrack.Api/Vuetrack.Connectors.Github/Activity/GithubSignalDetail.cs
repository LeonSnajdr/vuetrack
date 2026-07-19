using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Github.Activity;

// Connector-specific facts behind a GitHub ActivitySignal: the repository the commit landed in plus the
// commit itself. Opaque to the engine; the model reasons over its serialized shape and connector-aware
// code downcasts to it.
public sealed record GithubSignalDetail : IConnectorSignalDetail
{
    public required string RepoFullName { get; init; }

    public string? RepoName { get; init; }

    public string? Owner { get; init; }

    public required string Sha { get; init; }

    public string? ShortSha { get; init; }

    public string? MessageTitle { get; init; }

    public string? Message { get; init; }

    public string? AuthorLogin { get; init; }

    public string? SourceUrl { get; init; }
}
