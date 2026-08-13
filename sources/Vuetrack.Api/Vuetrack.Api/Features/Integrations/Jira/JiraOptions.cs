using Samhammer.Options.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;

namespace Vuetrack.Api.Features.Integrations.Jira;

[Option]
public class JiraOptions : OAuthOptions
{
    public required string ApiBaseUrl { get; init; }

    public required int PageSize { get; init; }

    public required int MaxPages { get; init; }

    public required int MaxConcurrency { get; init; }
}
