using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Jira.Connection;
using Vuetrack.Api.Features.Integrations.Jira.OAuth;
using Vuetrack.Api.Tests.Fakes;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Tests.Features.Integrations.Jira;

public class JiraConnectionContextFactoryTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task CreateAsync_CachesAccessToken_AndDoesNotRefreshTwice()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 3600);
        var factory = BuildFactory(oauth);

        var first = await factory.CreateAsync(UserId, CancellationToken.None);
        var second = await factory.CreateAsync(UserId, CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.AccessToken.Should().Be("access-0");
        second!.AccessToken.Should().Be("access-0");
        oauth.RefreshCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_MapsAttributesOntoTheContext()
    {
        var factory = BuildFactory(new FakeOAuthClient(expiresInSeconds: 3600));

        var context = await factory.CreateAsync(UserId, CancellationToken.None);

        context.Should().NotBeNull();
        context!.SiteUrl.Should().Be("https://acme.atlassian.net");
        context.CloudId.Should().Be("cloud-1");
        context.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenNoConnectionStored()
    {
        var factory = BuildFactoryWithoutConnection(new FakeOAuthClient(expiresInSeconds: 3600));

        var context = await factory.CreateAsync(UserId, CancellationToken.None);

        context.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_RefreshesAgain_AfterEvict()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 3600);
        var factory = BuildFactory(oauth);

        var first = await factory.CreateAsync(UserId, CancellationToken.None);
        await factory.EvictAsync(UserId, CancellationToken.None);
        var second = await factory.CreateAsync(UserId, CancellationToken.None);

        first!.AccessToken.Should().Be("access-0");
        second!.AccessToken.Should().Be("access-1");
        oauth.RefreshCalls.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_DoesNotCache_WhenTokenAlreadyExpired()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 30); // below the 60s buffer
        var factory = BuildFactory(oauth);

        await factory.CreateAsync(UserId, CancellationToken.None);
        await factory.CreateAsync(UserId, CancellationToken.None);

        oauth.RefreshCalls.Should().Be(2);
    }

    private static JiraConnectionContextFactory BuildFactory(FakeOAuthClient oauth)
    {
        var connection = new ConnectionModel
        {
            UserId = UserId,
            Key = IntegrationKey.Jira,
            EncryptedRefreshToken = "refresh-token",
            Attributes = new Dictionary<string, string>
            {
                [JiraConnectionAttributes.SiteUrl] = "https://acme.atlassian.net",
                [JiraConnectionAttributes.CloudId] = "cloud-1",
            },
        };

        return BuildFactory(oauth, connection);
    }

    private static JiraConnectionContextFactory BuildFactoryWithoutConnection(FakeOAuthClient oauth)
    {
        return BuildFactory(oauth, connection: null);
    }

    private static JiraConnectionContextFactory BuildFactory(FakeOAuthClient oauth, ConnectionModel? connection)
    {
        var repository = new FakeRepository(connection);
        var cache = new FusionCache(Options.Create(new FusionCacheOptions()));

        return new JiraConnectionContextFactory(repository, oauth, new PassthroughSecretProtector(), cache);
    }

    private sealed class FakeOAuthClient(int expiresInSeconds) : IJiraOAuthApiClient
    {
        public int RefreshCalls { get; private set; }

        public string BuildAuthorizationUrl(string state, string redirectUri) => string.Empty;

        public Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var response = new OAuthTokenResponse
            {
                AccessToken = $"access-{RefreshCalls}",
                RefreshToken = refreshToken,
                ExpiresInSeconds = expiresInSeconds,
            };
            RefreshCalls++;
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<JiraAccessibleResourceResponse>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class PassthroughSecretProtector : IConnectionSecretProtector
    {
        public string Protect(IntegrationKey key, string plaintext) => plaintext;

        public string Unprotect(IntegrationKey key, string ciphertext) => ciphertext;
    }

    private sealed class FakeRepository(ConnectionModel? connection) : IConnectionRepository
    {
        private ConnectionModel? Connection { get; set; } = connection;

        public Task<ConnectionModel?> GetAsync(string userId, IntegrationKey key, CancellationToken cancellationToken) => Task.FromResult(Connection);

        public Task UpsertAsync(string userId, IntegrationKey key, string encryptedRefreshToken, IReadOnlyDictionary<string, string> attributes, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task SetRefreshTokenAsync(string userId, IntegrationKey key, string encryptedRefreshToken, CancellationToken cancellationToken)
        {
            if (Connection is not null)
            {
                Connection.EncryptedRefreshToken = encryptedRefreshToken;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId, IntegrationKey key, CancellationToken cancellationToken)
        {
            Connection = null;
            return Task.CompletedTask;
        }

        public Task Save(ConnectionModel model)
        {
            Connection = model;
            return Task.CompletedTask;
        }

        public Task<ConnectionModel> GetById(string id) => throw new NotSupportedException();

        public Task<List<ConnectionModel>> GetAll() => throw new NotSupportedException();

        public Task Create(ConnectionModel model) => throw new NotSupportedException();

        public Task Delete(ConnectionModel model) => throw new NotSupportedException();

        public Task DeleteById(string id) => throw new NotSupportedException();

        public Task DeleteAll() => throw new NotSupportedException();
    }
}
