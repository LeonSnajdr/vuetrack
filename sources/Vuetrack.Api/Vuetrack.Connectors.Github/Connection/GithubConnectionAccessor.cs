using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Connectors.Github.Connection;

[Inject]
public class GithubConnectionAccessor : IGithubConnectionAccessor
{
    public GithubConnectionContainer? Current { get; set; }
}

public interface IGithubConnectionAccessor
{
    GithubConnectionContainer? Current { get; set; }
}
