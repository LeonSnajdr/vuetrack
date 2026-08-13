using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Contracts;

namespace Vuetrack.Api.Features.Integrations.Connections;

public abstract class IntegrationConnectionServiceBase(
    IOAuthApiClientBase oauthClient,
    IOptions<OAuthOptions> oauthOptions,
    IConnectionRepository repository,
    IOAuthTransactionRepository transactionRepository,
    IConnectionSessionFactory sessionFactory,
    IConnectionSecretProtector secretProtector,
    IIntegrationRegistry registry,
    ILogger logger)
    : IIntegrationConnectionService
{
    private static readonly TimeSpan TransactionLifetime = TimeSpan.FromMinutes(10);

    protected IOAuthApiClientBase OAuthClient { get; } = oauthClient;

    protected ILogger Logger { get; } = logger;

    private IConnectionRepository Repository { get; } = repository;

    private IOptions<OAuthOptions> OAuthOptions { get; } = oauthOptions;

    private IOAuthTransactionRepository TransactionRepository { get; } = transactionRepository;

    private IConnectionSessionFactory SessionFactory { get; } = sessionFactory;

    private IConnectionSecretProtector SecretProtector { get; } = secretProtector;

    private IIntegrationRegistry Registry { get; } = registry;

    public abstract IntegrationKey Key { get; }

    public async Task<ErrorOr<OAuthAuthorizeContract>> BuildAuthorizationAsync(string userId, string redirectUri, CancellationToken cancellationToken)
    {
        if (!OAuthOptions.Value.RedirectUris.Contains(redirectUri, StringComparer.Ordinal))
        {
            return Error.Validation(code: "OAuth.RedirectUriNotAllowed", description: "Redirect URI is not allowed.");
        }

        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var codeVerifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

        var transaction = new OAuthTransactionModel
        {
            State = state,
            UserId = userId,
            Key = Key,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier,
            DateExpires = DateTime.UtcNow.Add(TransactionLifetime),
        };
        await TransactionRepository.CreateAsync(transaction, cancellationToken);

        var url = OAuthClient.BuildAuthorizationUrl(state, redirectUri, codeChallenge);

        return new OAuthAuthorizeContract(url, state);
    }

    public async Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await Repository.GetAsync(userId, Key, cancellationToken);
        if (connection is null)
        {
            return new OAuthStatusContract(false, false);
        }

        var healthy = await CheckHealthAsync(userId, cancellationToken);

        return new OAuthStatusContract(true, healthy);
    }

    public async Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await TransactionRepository.ConsumeAsync(
                request.State,
                userId,
                Key,
                request.RedirectUri,
                DateTime.UtcNow,
                cancellationToken);
            if (transaction is null)
            {
                return Error.Validation(code: "OAuth.InvalidState", description: "OAuth transaction is invalid or expired.");
            }

            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, transaction.CodeVerifier, cancellationToken);

            var established = await EstablishAsync(token, cancellationToken);
            if (established.IsError)
            {
                return established.Errors;
            }

            var persisted = await PersistAsync(userId, token, established.Value, cancellationToken);
            if (!persisted)
            {
                return Error.Validation(code: "OAuth.MissingRefreshToken", description: "OAuth provider did not return a refresh token.");
            }

            return new OAuthConnectContract(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Integration} connect failed", Key);
            return Error.Unexpected();
        }
    }

    public async Task DisconnectAsync(string userId, CancellationToken cancellationToken)
    {
        await Repository.DeleteAsync(userId, Key, cancellationToken);
        await SessionFactory.EvictAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Probes the freshly issued token and returns the connection attributes to persist.
    /// A failure here means the credentials are unusable, so nothing is stored.
    /// </summary>
    protected abstract Task<ErrorOr<Dictionary<string, string>>> EstablishAsync(OAuthTokenResponse token, CancellationToken cancellationToken);

    private async Task<bool> CheckHealthAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var integration = Registry.Resolve<IIntegration>(Key);
            if (integration is null)
            {
                return false;
            }

            var validation = await integration.ValidateAsync(userId, cancellationToken);

            return !validation.IsError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "{Integration} status health check failed for user {UserId}", Key, userId);
            return false;
        }
    }

    private async Task<bool> PersistAsync(string userId, OAuthTokenResponse token, Dictionary<string, string> attributes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("{Integration} token response had no refresh token; connection not persisted", Key);
            return false;
        }

        var encryptedRefreshToken = SecretProtector.Protect(Key, token.RefreshToken);

        await Repository.UpsertAsync(userId, Key, encryptedRefreshToken, attributes, cancellationToken);
        await SessionFactory.EvictAsync(userId, cancellationToken);

        return true;
    }
}

public interface IIntegrationConnectionService
{
    IntegrationKey Key { get; }

    Task<ErrorOr<OAuthAuthorizeContract>> BuildAuthorizationAsync(string userId, string redirectUri, CancellationToken cancellationToken);

    Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken);

    Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId, CancellationToken cancellationToken);
}
