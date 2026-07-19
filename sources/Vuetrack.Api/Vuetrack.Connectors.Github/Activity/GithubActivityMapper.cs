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
        var owner = OwnerFromFullName(repoFullName);
        var shortSha = ShortSha(sha);

        var message = item.Commit?.Message ?? string.Empty;
        var truncatedMessage = message.Truncate(MaxMessageLength);
        var messageTitle = FirstLine(message);

        var authorLogin = item.Author?.Login;
        var started = item.Commit?.Author?.Date?.UtcDateTime ?? default;

        var detail = new GithubSignalDetail
        {
            RepoFullName = repoFullName,
            RepoName = item.Repository?.Name,
            Owner = owner,
            Sha = sha,
            ShortSha = shortSha,
            MessageTitle = messageTitle,
            Message = truncatedMessage,
            AuthorLogin = authorLogin,
            SourceUrl = item.HtmlUrl,
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

    private static string? OwnerFromFullName(string fullName)
    {
        var separator = fullName.IndexOf('/', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return null;
        }

        return fullName[..separator];
    }

    private static string? ShortSha(string sha)
    {
        if (string.IsNullOrEmpty(sha))
        {
            return null;
        }

        return sha.Length <= 7 ? sha : sha[..7];
    }

    private static string? FirstLine(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return null;
        }

        var newline = message.IndexOf('\n', StringComparison.Ordinal);
        var firstLine = newline < 0 ? message : message[..newline];
        return firstLine.Trim();
    }
}
