using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Connectors.Jira.Services;

/// <summary>
/// Holds the <see cref="JiraConnectionContainer"/> for the current DI scope (i.e. the current HTTP
/// request) so <see cref="ApiClients.JiraApiClient"/> can read the access token and <c>cloudId</c>
/// without threading them through the connector API. Registered <see cref="ServiceLifetime.Scoped"/>:
/// every service resolved within the request shares this one instance.
/// </summary>
/// <remarks>
/// Because this is a scoped reference (not an <see cref="AsyncLocal{T}"/>), a callee such as
/// <see cref="JiraConnectionContextFactory"/> can populate <see cref="Current"/> and the value is
/// visible to the caller. A background/hosted service driving a fetch must first open a scope
/// (<see cref="IServiceScopeFactory.CreateScope"/>); nothing does that today.
/// </remarks>
[Inject(Target.Matching, ServiceLifetime.Scoped)]
public class JiraConnectionAccessor : IJiraConnectionAccessor
{
    public JiraConnectionContainer? Current { get; set; }
}

public interface IJiraConnectionAccessor
{
    JiraConnectionContainer? Current { get; set; }
}
