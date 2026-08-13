using Samhammer.Options.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;

namespace Vuetrack.Api.Features.Integrations.Github;

[Option]
public class GithubOptions : OAuthOptions
{
    public required string ApiBaseUrl { get; init; }

    public required int PageSize { get; init; }

    public required int MaxPages { get; init; }
}
