using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Github.Activity;

public sealed record GithubSignalDetail : IConnectorSignalDetail
{
    public string? RepoName { get; init; }

    public string? Message { get; init; }
}
