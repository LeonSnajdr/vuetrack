namespace Vuetrack.Connectors.Abstractions;

[Flags]
public enum ConnectorCapabilities
{
    None = 0,

    Worklogs = 1 << 0,

    IssueActivity = 1 << 1,

    OAuth = 1 << 2,
}
