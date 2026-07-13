using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionAccessor : ITimetrackingConnectionAccessor
{
    public TimetrackingConnectionContainer? Current { get; set; }
}

public interface ITimetrackingConnectionAccessor
{
    TimetrackingConnectionContainer? Current { get; set; }
}
