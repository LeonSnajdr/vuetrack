using Samhammer.Options.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Jira;

[Option]
public class JiraOptions : OAuthOptions
{
    public required string ApiBaseUrl { get; init; }

    public required int PageSize { get; init; }

    public required int MaxPages { get; init; }
}
