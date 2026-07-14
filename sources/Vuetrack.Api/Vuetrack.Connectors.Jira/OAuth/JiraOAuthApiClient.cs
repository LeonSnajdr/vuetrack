using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Jira.OAuth;

[Inject]
public class JiraOAuthApiClient(HttpClient httpClient, IOptions<JiraOptions> options, ILogger<JiraOAuthApiClient> logger)
    : OAuthApiClientBase(httpClient, logger), IJiraOAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IOptions<JiraOptions> Options { get; } = options;

    protected override string ProviderName => "Jira";

    protected override string AuthorizeEndpoint => Options.Value.AuthorizeEndpoint;

    protected override string TokenEndpoint => Options.Value.TokenEndpoint;

    protected override string ClientId => Options.Value.ClientId;

    protected override string ClientSecret => Options.Value.ClientSecret;

    protected override string Scopes => Options.Value.Scopes;

    protected override string? AuthorizePrompt => "consent";

    protected override Parameters ExtraAuthorizeParameters => new() { { "audience", "api.atlassian.com" } };

    public async Task<IReadOnlyList<JiraAccessibleResourceResponse>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var uri = $"{Options.Value.ApiBaseUrl.TrimEnd('/')}/oauth/token/accessible-resources";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Jira accessible-resources returned {StatusCode}", (int)response.StatusCode);
            throw new JiraApiException(JiraApiErrorKind.Auth, $"Could not resolve accessible Jira sites ({(int)response.StatusCode}).");
        }

        var resources = await response.Content.ReadFromJsonAsync<List<JiraAccessibleResourceResponse>>(JsonOptions, cancellationToken);
        return resources ?? [];
    }
}

public interface IJiraOAuthApiClient
{
    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);

    Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraAccessibleResourceResponse>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken);
}
