namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public enum JiraApiErrorKind
{
    Auth,
    Transport,
}

public sealed class JiraApiException(JiraApiErrorKind kind, string message) : Exception(message)
{
    public JiraApiErrorKind Kind { get; } = kind;
}
