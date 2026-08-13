using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Timetracking.Api;

namespace Vuetrack.Api.Features.Integrations.Timetracking.OAuth;

[Inject]
public class TimetrackingOAuthApiClient(HttpClient httpClient, IOptions<TimetrackingOptions> options, ILogger<TimetrackingOAuthApiClient> logger) : OAuthApiClientBase(httpClient, logger, options), ITimetrackingOAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IOptions<TimetrackingOptions> Options { get; } = options;

    private ILogger<TimetrackingOAuthApiClient> Logger { get; } = logger;

    protected override IntegrationKey Key => IntegrationKey.Timetracking;

    public async Task<TimetrackingProfileResponse> GetProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        var uri = $"{Options.Value.ApiBaseUrl.TrimEnd('/')}/profile";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Timetracking profile endpoint returned {StatusCode}", (int)response.StatusCode);
            throw new InvalidOperationException($"Could not resolve the timetracking profile ({(int)response.StatusCode}).");
        }

        var profile = await response.Content.ReadFromJsonAsync<TimetrackingProfileResponse>(JsonOptions, cancellationToken);
        return profile ?? new TimetrackingProfileResponse();
    }
}

public interface ITimetrackingOAuthApiClient : IOAuthApiClientBase
{
    Task<TimetrackingProfileResponse> GetProfileAsync(string accessToken, CancellationToken cancellationToken);
}
