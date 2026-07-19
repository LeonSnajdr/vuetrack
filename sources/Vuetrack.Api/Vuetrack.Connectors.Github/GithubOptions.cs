using Samhammer.Options.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Github;

[Option]
public class GithubOptions : OAuthOptions
{
    public required string ApiBaseUrl { get; init; }

    public required int PageSize { get; init; }

    public required int MaxPages { get; init; }
}
