using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Github.Activity.Api;
using Vuetrack.Framework.Extensions;

namespace Vuetrack.Connectors.Github.Activity;

public static class GithubActivityMapper
{
    private const int MaxMessageLength = 500;

    public static ActivitySignal ToCommitSignal(GithubCommitItemResponse item)
    {
        var repoFullName = item.Repository?.FullName ?? string.Empty;
        var sha = item.Sha ?? string.Empty;
        var message = item.Commit?.Message ?? string.Empty;
        var truncatedMessage = message.Truncate(MaxMessageLength);
        var started = item.Commit?.Author?.Date ?? default;

        var detail = new GithubSignalDetail
        {
            RepoName = item.Repository?.Name,
            Message = truncatedMessage,
        };

        var externalId = $"{repoFullName}:commit:{sha}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Github,
            ExternalId = externalId,
            DateStarted = started,
            DateEnded = null,
            Kind = ActivityKind.Commit,
            Detail = detail,
        };
    }

}
