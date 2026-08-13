using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

public abstract class IntegrationApiClientBase(HttpClient httpClient, JsonSerializerOptions jsonOptions, ILogger logger)
{
    protected HttpClient HttpClient { get; } = httpClient;

    protected JsonSerializerOptions JsonOptions { get; } = jsonOptions;

    private ILogger Logger { get; } = logger;

    protected abstract IntegrationKey Key { get; }

    /// <summary>
    /// Wraps a failure into the provider's own exception type so callers can map it without knowing about HTTP.
    /// </summary>
    protected abstract Exception BuildException(bool isAuthFailure, string message);

    protected async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw BuildException(isAuthFailure: false, $"{Key} request failed: {ex.Message}");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw ToException(response);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            if (value is null)
            {
                throw BuildException(isAuthFailure: false, $"{Key} returned an empty response.");
            }

            return value;
        }
    }

    private Exception ToException(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        Logger.LogWarning("{Integration} API returned {StatusCode}", Key, statusCode);

        var isAuthFailure = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        var message = isAuthFailure
            ? $"{Key} rejected the credentials ({statusCode})."
            : $"{Key} request failed ({statusCode}).";

        return BuildException(isAuthFailure, message);
    }
}
