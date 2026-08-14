using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Features.Integrations.Github;

public sealed record GithubSignalDetail : IActivitySignalDetail
{
    public string? RepoName { get; init; }

    public string? Message { get; init; }
}
