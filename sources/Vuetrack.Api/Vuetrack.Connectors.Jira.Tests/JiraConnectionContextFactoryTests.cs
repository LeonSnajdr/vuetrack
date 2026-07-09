using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Vuetrack.Connectors.Jira.ApiClients;
using Vuetrack.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Jira.Models;
using Vuetrack.Connectors.Jira.Repositories;
using Vuetrack.Connectors.Jira.Services;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

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

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("access-0", first!.AccessToken);
        Assert.Equal("access-0", second!.AccessToken);
        Assert.Equal(1, oauth.RefreshCalls);
    }

    [Fact]
    public async Task CreateAsync_RefreshesAgain_AfterEvict()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 3600);
        var factory = BuildFactory(oauth);

        var first = await factory.CreateAsync(UserId, CancellationToken.None);
        factory.Evict(UserId);
        var second = await factory.CreateAsync(UserId, CancellationToken.None);

        Assert.Equal("access-0", first!.AccessToken);
        Assert.Equal("access-1", second!.AccessToken);
        Assert.Equal(2, oauth.RefreshCalls);
    }

    [Fact]
    public async Task CreateAsync_DoesNotCache_WhenTokenAlreadyExpired()
    {
        var oauth = new FakeOAuthClient(expiresInSeconds: 30); // below the 60s buffer
        var factory = BuildFactory(oauth);

        await factory.CreateAsync(UserId, CancellationToken.None);
        await factory.CreateAsync(UserId, CancellationToken.None);

        Assert.Equal(2, oauth.RefreshCalls);
    }

    private static JiraConnectionContextFactory BuildFactory(FakeOAuthClient oauth)
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

        var cache = new MemoryCache(new MemoryCacheOptions());
        return new JiraConnectionContextFactory(repository, oauth, new PassthroughSecretProtector(), cache);
    }

    private sealed class FakeOAuthClient(int expiresInSeconds) : IJiraOAuthApiClient
    {
        public int RefreshCalls { get; private set; }

        public string BuildAuthorizationUrl(string state, string redirectUri) => string.Empty;

        public Task<JiraTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JiraTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var response = new JiraTokenResponse
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
