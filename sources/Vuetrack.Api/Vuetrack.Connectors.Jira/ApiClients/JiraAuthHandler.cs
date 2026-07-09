using System.Net.Http.Headers;
using Vuetrack.Connectors.Jira.Services;

namespace Vuetrack.Connectors.Jira.ApiClients;

/// <summary>
/// Attaches the current user's Jira access token as a Bearer header to every outgoing request on the
/// <see cref="JiraApiClient"/> typed client. The token is read from the ambient
/// <see cref="IJiraConnectionAccessor"/>, so callers no longer thread it through the connector API.
/// </summary>
public class JiraAuthHandler(IJiraConnectionAccessor accessor) : DelegatingHandler
{
    private IJiraConnectionAccessor Accessor { get; } = accessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var connection = Accessor.Current;
        if (connection is not null && !string.IsNullOrEmpty(connection.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
