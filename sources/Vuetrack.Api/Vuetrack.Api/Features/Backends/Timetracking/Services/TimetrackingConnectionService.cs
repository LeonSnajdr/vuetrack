using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Backends.Timetracking.Contracts;
using Vuetrack.Backends.Abstractions;
using Vuetrack.Backends.Timetracking;
using Vuetrack.Backends.Timetracking.Api;
using Vuetrack.Backends.Timetracking.Connection;
using Vuetrack.Backends.Timetracking.OAuth;
using Vuetrack.Framework.Errors;
using Vuetrack.OAuth;

namespace Vuetrack.Api.Features.Backends.Timetracking.Services;

[Inject]
public class TimetrackingConnectionService(
    IBackendRegistry registry,
    ITimetrackingOAuthApiClient oauthClient,
    ITimetrackingApiClient apiClient,
    ITimetrackingConnectionRepository repository,
    ISecretProtector secretProtector,
    ITimetrackingConnectionAccessor accessor,
    ITimetrackingConnectionContextFactory contextFactory,
    ILogger<TimetrackingConnectionService> logger) : ITimetrackingConnectionService
{
    private const string AuthMode = "oauth2-3lo";

    private IBackendRegistry Registry { get; } = registry;

    private ITimetrackingOAuthApiClient OAuthClient { get; } = oauthClient;

    private ITimetrackingApiClient ApiClient { get; } = apiClient;

    private ITimetrackingConnectionRepository Repository { get; } = repository;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    private ITimetrackingConnectionContextFactory ContextFactory { get; } = contextFactory;

    private ILogger<TimetrackingConnectionService> Logger { get; } = logger;

    public TimetrackingAuthorizeContract BuildAuthorization(string redirectUri)
    {
        var state = Guid.NewGuid().ToString("N");
        var url = OAuthClient.BuildAuthorizationUrl(state, redirectUri);
        return new TimetrackingAuthorizeContract(url, state);
    }

    public async Task<TimetrackingStatusContract> GetStatusAsync(string userId)
    {
        var connection = await Repository.GetByUserId(userId);
        return new TimetrackingStatusContract(connection is { Enabled: true });
    }

    public async Task<ErrorOr<TimetrackingConnectContract>> ConnectAsync(string userId, TimetrackingConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);

            // Publish the freshly-minted access token so the api client can resolve the profile + validate.
            Accessor.Current = new TimetrackingConnectionContainer
            {
                UserId = userId,
                AccessToken = token.AccessToken,
            };

            var profile = await ApiClient.GetProfileAsync(cancellationToken);
            var externalUserId = profile.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

            Accessor.Current = Accessor.Current with { ExternalUserId = externalUserId };

            var backend = Registry.Resolve(TimetrackingBackend.Key) ?? throw new InvalidOperationException("Timetracking backend is not registered.");

            var validation = await backend.ValidateAsync(cancellationToken);
            if (validation.IsError)
            {
                return validation.Errors;
            }

            await PersistConnection(userId, externalUserId, token);
            return new TimetrackingConnectContract(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Timetracking connect failed");
            return Error.Unexpected();
        }
    }

    public async Task DisconnectAsync(string userId)
    {
        var connection = await Repository.GetByUserId(userId);
        if (connection is not null)
        {
            await Repository.Delete(connection);
        }

        ContextFactory.Evict(userId);
    }

    private async Task PersistConnection(string userId, string externalUserId, OAuthTokenResponse token)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("Timetracking token response had no refresh token; connection not persisted");
            return;
        }

        var encryptedRefreshToken = SecretProtector.Protect(token.RefreshToken);

        await Repository.UpsertConnectionAsync(userId, AuthMode, encryptedRefreshToken, externalUserId);
    }
}

public interface ITimetrackingConnectionService
{
    TimetrackingAuthorizeContract BuildAuthorization(string redirectUri);

    Task<TimetrackingStatusContract> GetStatusAsync(string userId);

    Task<ErrorOr<TimetrackingConnectContract>> ConnectAsync(string userId, TimetrackingConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId);
}
