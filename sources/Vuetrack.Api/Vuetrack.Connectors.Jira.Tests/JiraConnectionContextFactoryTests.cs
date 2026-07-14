using System.Collections.Generic;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Vuetrack.Connectors.Jira.Connection;
using Vuetrack.Connectors.Jira.OAuth;
using Vuetrack.OAuth;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraConnectionContextFactoryTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task CreateAsync_CachesAccessToken_AndDoesNotRefreshTwice()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 3600);
        var accessor = new JiraConnectionAccessor();
        var factory = BuildFactory(oauth, accessor);

        var first = await factory.CreateAsync(UserId, CancellationToken.None);
        var second = await factory.CreateAsync(UserId, CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.AccessToken.Should().Be("access-0");
        second!.AccessToken.Should().Be("access-0");
        oauth.RefreshCalls.Should().Be(1);
        // CreateAsync publishes the resolved connection on the scoped accessor (cache-hit path too).
        accessor.Current.Should().BeSameAs(second);
    }

    [Fact]
    public async Task CreateAsync_RefreshesAgain_AfterEvict()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 3600);
        var factory = BuildFactory(oauth);

        var first = await factory.CreateAsync(UserId, CancellationToken.None);
        factory.Evict(UserId);
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

    private static JiraConnectionContextFactory BuildFactory(FakeOAuthClient oauth, JiraConnectionAccessor? accessor = null)
    {
        var repository = new FakeRepository(new JiraConnectionModel
        {
            UserId = UserId,
            SiteUrl = "https://acme.atlassian.net",
            CloudId = "cloud-1",
            AuthMode = "oauth2-3lo",
            EncryptedRefreshToken = "refresh-token",
            Enabled = true,
        });

        var cache = new FusionCache(Options.Create(new FusionCacheOptions()));
        return new JiraConnectionContextFactory(repository, oauth, new PassthroughSecretProtector(), accessor ?? new JiraConnectionAccessor(), cache);
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

    private sealed class PassthroughSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;

        public string Unprotect(string ciphertext) => ciphertext;
    }

    private sealed class FakeRepository(JiraConnectionModel? connection) : IJiraConnectionRepository
    {
        private JiraConnectionModel? Connection { get; set; } = connection;

        public Task<JiraConnectionModel?> GetByUserId(string userId) => Task.FromResult(Connection);

        public Task UpsertConnectionAsync(string userId, string siteUrl, string cloudId, string authMode, string encryptedRefreshToken)
            => throw new NotSupportedException();

        public Task SetRefreshTokenAsync(string userId, string encryptedRefreshToken)
        {
            if (Connection is not null)
            {
                Connection.EncryptedRefreshToken = encryptedRefreshToken;
            }

            return Task.CompletedTask;
        }

        public Task Save(JiraConnectionModel model)
        {
            Connection = model;
            return Task.CompletedTask;
        }

        public Task<JiraConnectionModel> GetById(string id) => throw new NotSupportedException();

        public Task<List<JiraConnectionModel>> GetAll() => throw new NotSupportedException();

        public Task Create(JiraConnectionModel model) => throw new NotSupportedException();

        public Task Delete(JiraConnectionModel model) => throw new NotSupportedException();

        public Task DeleteById(string id) => throw new NotSupportedException();

        public Task DeleteAll() => throw new NotSupportedException();
    }
}
