using System.Globalization;
using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;
using Vuetrack.Backends.Timetracking;
using Vuetrack.Backends.Timetracking.Api;
using Vuetrack.Backends.Timetracking.Connection;
using Vuetrack.Backends.Timetracking.OAuth;
using Vuetrack.OAuth;
using Vuetrack.OAuth.Contractrs;

namespace Vuetrack.Api.Features.Backends.Timetracking.Services;

[Inject(Target.All)]
public class TimetrackingConnectionService(
    IBackendRegistry registry,
    IBackendResolver resolver,
    ITimetrackingOAuthApiClient oauthClient,
    ITimetrackingApiClient apiClient,
    ITimetrackingConnectionRepository repository,
    ITimetrackingSecretProtector secretProtector,
    ITimetrackingConnectionAccessor accessor,
    ITimetrackingConnectionContextFactory contextFactory,
    ILogger<TimetrackingConnectionService> logger)
    : OAuthConnectionServiceBase<TimetrackingConnectionModel>(oauthClient, repository, contextFactory, secretProtector, logger), ITimetrackingConnectionService
{
    private const string AuthMode = "oauth2-3lo";

    private IBackendRegistry Registry { get; } = registry;

    private IBackendResolver Resolver { get; } = resolver;

    private ITimetrackingApiClient ApiClient { get; } = apiClient;

    private ITimetrackingConnectionRepository Repository { get; } = repository;

    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    protected override string ProviderName => nameof(BackendKey.Timetracking);

    public BackendKey Key => BackendKey.Timetracking;

    public async Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);

            Accessor.Current = new TimetrackingConnectionContainer
            {
                UserId = userId,
                AccessToken = token.AccessToken,
            };

            var profile = await ApiClient.GetProfileAsync(cancellationToken);
            var externalUserId = profile.Id.ToString(CultureInfo.InvariantCulture);

            Accessor.Current = Accessor.Current with { ExternalUserId = externalUserId };

            var backend = Registry.Resolve(TimetrackingBackend.Key) ?? throw new InvalidOperationException("Timetracking backend is not registered.");

            var validation = await backend.ValidateAsync(cancellationToken);
            if (validation.IsError)
            {
                return validation.Errors;
            }

            await PersistConnection(userId, externalUserId, token);
            return new OAuthConnectContract(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Timetracking connect failed");
            return Error.Unexpected();
        }
    }

    protected override async Task<bool> CheckHealthAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(TimetrackingBackend.Key, userId, cancellationToken);
            if (resolved.IsError)
            {
                return false;
            }

            var validation = await resolved.Value.ValidateAsync(cancellationToken);
            return !validation.IsError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Timetracking status health check failed for user {UserId}", userId);
            return false;
        }
    }

    private async Task PersistConnection(string userId, string externalUserId, OAuthTokenResponse token)
    {
        var encryptedRefreshToken = ProtectRefreshToken(token);
        if (encryptedRefreshToken is null)
        {
            return;
        }

        await Repository.UpsertConnectionAsync(userId, AuthMode, encryptedRefreshToken, externalUserId);
    }
}

public interface ITimetrackingConnectionService : IBackendConnectionService
{
    OAuthAuthorizeContract BuildAuthorization(string redirectUri);

    Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId);
}
