using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Connectors.Jira.Services;

/// <summary>
/// Holds the <see cref="JiraConnection"/> for the current logical operation so the
/// <c>JiraAuthHandler</c> and <see cref="ApiClients.JiraApiClient"/> can read it ambiently.
/// Backed by <see cref="AsyncLocal{T}"/> (not DI scope): a <see cref="System.Net.Http.DelegatingHandler"/>
/// is long-lived and cannot safely capture a scoped service, but the async-local value flows
/// with the call chain that set it.
/// </summary>
[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class JiraConnectionAccessor : IJiraConnectionAccessor
{
    private readonly AsyncLocal<JiraConnection?> current = new();

    public JiraConnection? Current
    {
        get => current.Value;
        set => current.Value = value;
    }
}

public interface IJiraConnectionAccessor
{
    JiraConnection? Current { get; set; }
}
