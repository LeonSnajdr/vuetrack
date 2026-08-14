using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using ErrorOr;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Contracts;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Integrations;

public class OAuthConnectionSecurityTests
{
    private const string AllowedRedirectUri = "https://app.example.test/oauth/callback";

    [Fact]
    public async Task BuildAuthorizationAsync_BindsTransactionAndPkceChallenge()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.BuildAuthorizationAsync("user-1", AllowedRedirectUri, CancellationToken.None);

        result.IsError.Should().BeFalse();
        fixture.Transactions.Created.Should().NotBeNull();
        var transaction = fixture.Transactions.Created!;
        transaction.UserId.Should().Be("user-1");
        transaction.Key.Should().Be(IntegrationKey.Jira);
        transaction.RedirectUri.Should().Be(AllowedRedirectUri);
        transaction.State.Should().Be(result.Value.State);
        transaction.DateExpires.Should().BeAfter(DateTime.UtcNow.AddMinutes(9));
        var expectedChallenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(transaction.CodeVerifier)));
        fixture.OAuth.CodeChallenge.Should().Be(expectedChallenge);
    }

    [Fact]
    public async Task BuildAuthorizationAsync_RejectsRedirectUriOutsideExactAllowlist()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.BuildAuthorizationAsync("user-1", AllowedRedirectUri + "/other", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OAuth.RedirectUriNotAllowed");
        fixture.Transactions.Created.Should().BeNull();
    }

    [Fact]
    public async Task ConnectAsync_ConsumesStateAndUsesBoundPkceVerifier()
    {
        var fixture = new Fixture();
        var authorization = await fixture.Service.BuildAuthorizationAsync("user-1", AllowedRedirectUri, CancellationToken.None);
        var request = Request(authorization.Value.State);

        var first = await fixture.Service.ConnectAsync("user-1", request, CancellationToken.None);
        var replay = await fixture.Service.ConnectAsync("user-1", request, CancellationToken.None);

        first.IsError.Should().BeFalse();
        replay.IsError.Should().BeTrue();
        replay.FirstError.Code.Should().Be("OAuth.InvalidState");
        fixture.OAuth.CodeVerifier.Should().Be(fixture.Transactions.LastConsumed!.CodeVerifier);
        fixture.Connections.UpsertCalls.Should().Be(1);
    }

    [Fact]
    public async Task ConnectAsync_RejectsStateBoundToAnotherUser()
    {
        var fixture = new Fixture();
        var authorization = await fixture.Service.BuildAuthorizationAsync("user-1", AllowedRedirectUri, CancellationToken.None);

        var result = await fixture.Service.ConnectAsync("user-2", Request(authorization.Value.State), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OAuth.InvalidState");
        fixture.OAuth.ExchangeCalls.Should().Be(0);
    }

    [Fact]
    public async Task ConnectAsync_ReturnsError_WhenRefreshTokenIsMissing()
    {
        var fixture = new Fixture(refreshToken: null);
        var authorization = await fixture.Service.BuildAuthorizationAsync("user-1", AllowedRedirectUri, CancellationToken.None);

        var result = await fixture.Service.ConnectAsync("user-1", Request(authorization.Value.State), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OAuth.MissingRefreshToken");
        fixture.Connections.UpsertCalls.Should().Be(0);
    }

    [Fact]
    public async Task OAuthApiClient_SendsPkceChallengeAndVerifier()
    {
        string? tokenRequestBody = null;
        var handler = new AsyncHttpMessageHandler(async request =>
        {
            tokenRequestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"access-token","refresh_token":"refresh-token","expires_in":3600}""", Encoding.UTF8, "application/json"),
            };
        });
        var options = Options.Create<OAuthOptions>(CreateOptions());
        var client = new TestOAuthApiClient(new HttpClient(handler), options);

        var authorizationUrl = client.BuildAuthorizationUrl("state-value", AllowedRedirectUri, "challenge-value");
        await client.ExchangeCodeAsync("code-value", AllowedRedirectUri, "verifier-value", CancellationToken.None);

        authorizationUrl.Should().Contain("code_challenge=challenge-value");
        authorizationUrl.Should().Contain("code_challenge_method=S256");
        tokenRequestBody.Should().Contain("code_verifier=verifier-value");
    }

    private static OAuthConnectCreateContract Request(string state) => new()
    {
        Code = "authorization-code",
        State = state,
        RedirectUri = AllowedRedirectUri,
    };

    private static TestOAuthOptions CreateOptions() => new()
    {
        AuthorizeEndpoint = "https://provider.example.test/authorize",
        TokenEndpoint = "https://provider.example.test/token",
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUris = [AllowedRedirectUri],
        Scopes = "read offline_access",
    };

    private sealed class Fixture
    {
        public Fixture(string? refreshToken = "refresh-token")
        {
            OAuth = new FakeOAuthClient(refreshToken);
            Transactions = new FakeTransactionRepository();
            Connections = new FakeConnectionRepository();
            var options = Options.Create<OAuthOptions>(CreateOptions());
            Service = new TestConnectionService(
                OAuth,
                options,
                Connections,
                Transactions,
                new FakeSessionFactory(),
                new PassthroughSecretProtector(),
                new EmptyRegistry());
        }

        public FakeOAuthClient OAuth { get; }

        public FakeTransactionRepository Transactions { get; }

        public FakeConnectionRepository Connections { get; }

        public TestConnectionService Service { get; }
    }

    private sealed class TestOAuthOptions : OAuthOptions;

    private sealed class TestOAuthApiClient(HttpClient httpClient, IOptions<OAuthOptions> options)
        : OAuthApiClientBase(httpClient, NullLogger<TestOAuthApiClient>.Instance, options)
    {
        protected override IntegrationKey Key => IntegrationKey.Jira;
    }

    private sealed class AsyncHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    }

    private sealed class TestConnectionService(
        IOAuthApiClientBase oauthClient,
        IOptions<OAuthOptions> oauthOptions,
        IConnectionRepository repository,
        IOAuthTransactionRepository transactionRepository,
        IConnectionSessionFactory sessionFactory,
        IConnectionSecretProtector secretProtector,
        IIntegrationRegistry registry)
        : IntegrationConnectionServiceBase(
            oauthClient,
            oauthOptions,
            repository,
            transactionRepository,
            sessionFactory,
            secretProtector,
            registry,
            NullLogger<TestConnectionService>.Instance)
    {
        public override IntegrationKey Key => IntegrationKey.Jira;

        protected override Task<ErrorOr<Dictionary<string, string>>> EstablishAsync(OAuthTokenResponse token, CancellationToken cancellationToken)
            => Task.FromResult<ErrorOr<Dictionary<string, string>>>(new Dictionary<string, string> { ["account"] = "test" });
    }

    private sealed class FakeOAuthClient(string? refreshToken) : IOAuthApiClientBase
    {
        public string? CodeChallenge { get; private set; }

        public string? CodeVerifier { get; private set; }

        public int ExchangeCalls { get; private set; }

        public string BuildAuthorizationUrl(string state, string redirectUri, string codeChallenge)
        {
            CodeChallenge = codeChallenge;
            return "https://provider.example.test/authorize";
        }

        public Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken)
        {
            ExchangeCalls++;
            CodeVerifier = codeVerifier;
            return Task.FromResult(new OAuthTokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = refreshToken,
                ExpiresInSeconds = 3600,
            });
        }

        public Task<OAuthTokenResponse> RefreshAsync(string token, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeTransactionRepository : IOAuthTransactionRepository
    {
        public OAuthTransactionModel? Created { get; private set; }

        public OAuthTransactionModel? LastConsumed { get; private set; }

        public Task CreateAsync(OAuthTransactionModel transaction, CancellationToken cancellationToken)
        {
            Created = transaction;
            return Task.CompletedTask;
        }

        public Task<OAuthTransactionModel?> ConsumeAsync(string state, string userId, IntegrationKey key, string redirectUri, DateTime now, CancellationToken cancellationToken)
        {
            if (Created is null || Created.State != state || Created.UserId != userId || Created.Key != key || Created.RedirectUri != redirectUri || Created.DateExpires <= now)
            {
                return Task.FromResult<OAuthTransactionModel?>(null);
            }

            LastConsumed = Created;
            Created = null;
            return Task.FromResult<OAuthTransactionModel?>(LastConsumed);
        }

        public Task Save(OAuthTransactionModel model) => throw new NotSupportedException();

        public Task<OAuthTransactionModel> GetById(string id) => throw new NotSupportedException();

        public Task<List<OAuthTransactionModel>> GetAll() => throw new NotSupportedException();

        public Task Create(OAuthTransactionModel model) => throw new NotSupportedException();

        public Task Delete(OAuthTransactionModel model) => throw new NotSupportedException();

        public Task DeleteById(string id) => throw new NotSupportedException();

        public Task DeleteAll() => throw new NotSupportedException();
    }

    private sealed class FakeConnectionRepository : IConnectionRepository
    {
        public int UpsertCalls { get; private set; }

        public Task<ConnectionModel?> GetAsync(string userId, IntegrationKey key, CancellationToken cancellationToken) => Task.FromResult<ConnectionModel?>(null);

        public Task UpsertAsync(string userId, IntegrationKey key, string encryptedRefreshToken, IReadOnlyDictionary<string, string> attributes, CancellationToken cancellationToken)
        {
            UpsertCalls++;
            return Task.CompletedTask;
        }

        public Task<bool> TrySetRefreshTokenAsync(string userId, IntegrationKey key, string currentEncryptedRefreshToken, string rotatedEncryptedRefreshToken, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task DeleteAsync(string userId, IntegrationKey key, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Save(ConnectionModel model) => throw new NotSupportedException();

        public Task<ConnectionModel> GetById(string id) => throw new NotSupportedException();

        public Task<List<ConnectionModel>> GetAll() => throw new NotSupportedException();

        public Task Create(ConnectionModel model) => throw new NotSupportedException();

        public Task Delete(ConnectionModel model) => throw new NotSupportedException();

        public Task DeleteById(string id) => throw new NotSupportedException();

        public Task DeleteAll() => throw new NotSupportedException();
    }

    private sealed class FakeSessionFactory : IConnectionSessionFactory
    {
        public Task EvictAsync(string userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PassthroughSecretProtector : IConnectionSecretProtector
    {
        public string Protect(IntegrationKey key, string plaintext) => plaintext;

        public string Unprotect(IntegrationKey key, string ciphertext) => ciphertext;
    }

    private sealed class EmptyRegistry : IIntegrationRegistry
    {
        public T? Resolve<T>(IntegrationKey key)
            where T : class, IIntegration => null;

        public IReadOnlyList<T> ResolveAll<T>()
            where T : class, IIntegration => [];
    }
}
