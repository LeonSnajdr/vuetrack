using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Integrations.Github;
using Vuetrack.Api.Features.Integrations.Github.Api;

namespace Vuetrack.Api.Features.Integrations.Github.Internal;

public static class GithubActivityMapper
{
    private const int MaxMessageLength = 500;

    public static ActivitySignal ToCommitSignal(this GithubCommitItemResponse item)
    {
        var repoFullName = item.Repository?.FullName ?? string.Empty;
        var sha = item.Sha ?? string.Empty;
        var message = item.Commit?.Message ?? string.Empty;
        var truncatedMessage = message.Length > MaxMessageLength ? message[..MaxMessageLength] : message;
        var started = item.Commit?.Author?.Date?.ToUniversalTime() ?? default;

        var detail = new GithubSignalDetail
        {
            RepoName = item.Repository?.Name,
            Message = truncatedMessage,
        };

        var externalId = $"{repoFullName}:commit:{sha}";

        return new ActivitySignal
        {
            Key = IntegrationKey.Github,
            ExternalId = externalId,
            DateStarted = started,
            DateEnded = null,
            Kind = ActivityKind.Commit,
            Detail = detail,
        };
    }
}
