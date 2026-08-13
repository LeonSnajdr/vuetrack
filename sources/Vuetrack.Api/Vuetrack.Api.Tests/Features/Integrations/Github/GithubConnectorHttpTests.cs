using System.Net;
using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Integrations.Github;
using Vuetrack.Api.Features.Integrations.Github.Api;
using Vuetrack.Api.Features.Integrations.Github.Connection;
using Vuetrack.Api.Features.Integrations.Github.Internal;
using Vuetrack.Api.Tests.Fakes;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Integrations.Github;

public class GithubConnectorHttpTests
{
    private const string Login = "octocat";

    private const string User = """{ "login": "octocat", "id": 42 }""";

    private const string EmptyCommits = """{ "total_count": 0, "items": [] }""";

    private static readonly DateRange Container = new()
    {
        From = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        To = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public async Task FetchAsync_HappyPath_ReturnsCommitSignalWithTypedMetadata()
    {
        const string commits = """
        {
          "total_count": 1,
          "items": [
            {
              "sha": "abc1234def5678",
              "commit": {
                "author": { "date": "2026-07-01T09:00:00Z" },
                "message": "fix: correct the widget\n\nlong body here"
              },
              "repository": { "name": "widgets", "full_name": "acme/widgets" }
            }
          ]
        }
        """;

        var connector = BuildConnector(Responder(commits));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.ExternalId.Should().Be("acme/widgets:commit:abc1234def5678");
        signal.Kind.Should().Be(ActivityKind.Commit);
        signal.DateEnded.Should().BeNull();
        var detail = Detail(signal);
        detail.RepoName.Should().Be("widgets");
        detail.Message.Should().Be("fix: correct the widget\n\nlong body here");
    }

    [Fact]
    public async Task FetchAsync_NormalizesOffsetTimestampsToUtc()
    {
        const string commits = """
        {
          "total_count": 1,
          "items": [
            {
              "sha": "abc",
              "commit": { "author": { "date": "2026-07-01T09:00:00+02:00" }, "message": "chore" },
              "repository": { "name": "widgets", "full_name": "acme/widgets" }
            }
          ]
        }
        """;

        var connector = BuildConnector(Responder(commits));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc));
        signal.DateStarted.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task FetchAsync_PagesUntilShortPage()
    {
        const string page1 = """
        {
          "total_count": 3,
          "items": [
            { "sha": "s1", "commit": { "author": { "date": "2026-07-01T09:00:00Z" }, "message": "one" }, "repository": { "name": "widgets", "full_name": "acme/widgets" } },
            { "sha": "s2", "commit": { "author": { "date": "2026-07-01T10:00:00Z" }, "message": "two" }, "repository": { "name": "widgets", "full_name": "acme/widgets" } }
          ]
        }
        """;
        const string page2 = """
        {
          "total_count": 3,
          "items": [
            { "sha": "s3", "commit": { "author": { "date": "2026-07-01T11:00:00Z" }, "message": "three" }, "repository": { "name": "widgets", "full_name": "acme/widgets" } }
          ]
        }
        """;

        var connector = BuildConnector(request =>
        {
            AssertAuth(request);
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/user"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, User);
            }

            var body = uri.Contains("&page=2") ? page2 : page1;
            return StubHttpMessageHandler.Json(HttpStatusCode.OK, body);
        }, pageSize: 2);

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task FetchAsync_ReturnsUnauthorized_On401()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task FetchAsync_ReturnsFailure_On429()
    {
        var connector = BuildConnector(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/user"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, User);
            }

            var response = StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, "{}");
            response.Headers.Add("Retry-After", "30");
            return response;
        });

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_WhenUserSucceeds()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.OK, User));

        var result = await connector.ValidateAsync("user-1", CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsUnauthorized_WhenUserRejected()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.Forbidden, "{}"));

        var result = await connector.ValidateAsync("user-1", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> Responder(string commits)
    {
        return request =>
        {
            AssertAuth(request);

            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/user"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, User);
            }

            if (uri.Contains("search/commits"))
            {
                uri.Should().Contain($"author%3A{Login}");
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, commits);
            }

            return StubHttpMessageHandler.Json(HttpStatusCode.OK, EmptyCommits);
        };
    }

    private static void AssertAuth(HttpRequestMessage request)
    {
        request.Headers.Authorization?.Scheme.Should().Be("Bearer");
        request.Headers.Authorization?.Parameter.Should().Be("access-token");
        request.Headers.UserAgent.ToString().Should().Contain("Vuetrack");
    }

    private static GithubSignalDetail Detail(ActivitySignal signal)
    {
        return signal.Detail.Should().BeOfType<GithubSignalDetail>().Which;
    }

    private static GithubConnector BuildConnector(Func<HttpRequestMessage, HttpResponseMessage> responder, int pageSize = 100)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var options = Options.Create(new GithubOptions
        {
            ApiBaseUrl = "https://api.github.com",
            AuthorizeEndpoint = "https://github.com/login/oauth/authorize",
            TokenEndpoint = "https://github.com/login/oauth/access_token",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = string.Empty,
            PageSize = pageSize,
            MaxPages = 10,
        });
        var context = new GithubConnectionContext
        {
            UserId = "user-1",
            AccessToken = "access-token",
            Login = Login,
        };
        var client = new GithubApiClient(httpClient, options, NullLogger<GithubApiClient>.Instance);
        return new GithubConnector(client, new StubGithubConnectionContextFactory(context));
    }
}
