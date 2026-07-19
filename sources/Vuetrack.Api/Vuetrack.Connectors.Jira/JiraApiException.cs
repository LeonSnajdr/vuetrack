namespace Vuetrack.Connectors.Jira;

public enum JiraApiErrorKind
{
    Auth,
    Transport,
}

public sealed class JiraApiException(JiraApiErrorKind kind, string message) : Exception(message)
{
    public JiraApiErrorKind Kind { get; } = kind;
}
