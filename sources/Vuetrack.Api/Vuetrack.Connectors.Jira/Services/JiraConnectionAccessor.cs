using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Connectors.Jira.Services;

[Inject]
public class JiraConnectionAccessor : IJiraConnectionAccessor
{
    public JiraConnectionContainer? Current { get; set; }
}

public interface IJiraConnectionAccessor
{
    JiraConnectionContainer? Current { get; set; }
}
