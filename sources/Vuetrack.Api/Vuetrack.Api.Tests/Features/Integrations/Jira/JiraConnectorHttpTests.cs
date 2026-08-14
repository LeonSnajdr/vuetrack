using System.Net;
using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Integrations.Jira;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Connection;
using Vuetrack.Api.Features.Integrations.Jira.Internal;
using Vuetrack.Api.Tests.Fakes;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Integrations.Jira;

public class JiraConnectorHttpTests
{
    private const string SiteUrl = "https://acme.atlassian.net";

    private const string Myself = """{ "accountId": "acc-1" }""";

    private const string EmptyWorklogs = """{ "worklogs": [], "total": 0 }""";

    private const string EmptyComments = """{ "comments": [], "total": 0 }""";

    private const string EmptyChangelog = """{ "issueChangeLogs": [] }""";

    private const string SearchProj1 = """
    {
      "issues": [
        { "id": "1001", "key": "PROJ-1", "fields": {
          "summary": "Fix login",
          "issuetype": { "name": "Bug" },
          "status": { "name": "In Progress" },
          "parent": { "key": "PROJ-9", "fields": { "summary": "Login epic" } },
          "project": { "key": "PROJ", "name": "Project" }
        } }
      ],
      "isLast": true
    }
    """;

    private static readonly DateRange Container = new()
    {
        From = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        To = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public async Task FetchAsync_HappyPath_ReturnsWorklogSignalWithTypedMetadata()
    {
        const string worklog = """
        {
          "worklogs": [
            {
              "id": "100",
              "author": { "accountId": "acc-1" },
              "started": "2026-07-01T09:00:00.000+00:00",
              "timeSpentSeconds": 3600,
              "comment": { "type": "doc", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "worked on the fix" } ] } ] }
            }
          ],
          "total": 1
        }
        """;

        var connector = BuildConnector(Responder(SearchProj1, worklog));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.Kind.Should().Be(ActivityKind.Worklog);
        var detail = Detail(signal);
        detail.IssueKey.Should().Be("PROJ-1");
        detail.Title.Should().Be("Fix login");
        detail.IssueType.Should().Be("Bug");
        detail.ParentKey.Should().Be("PROJ-9");
        detail.ParentTitle.Should().Be("Login epic");
    }

    [Fact]
    public async Task FetchAsync_NormalizesOffsetTimestampsToUtc()
    {
        const string worklog = """
        {
          "worklogs": [
            { "id": "100", "author": { "accountId": "acc-1" }, "started": "2026-07-01T09:00:00.000+02:00", "timeSpentSeconds": 3600 }
          ],
          "total": 1
        }
        """;

        var connector = BuildConnector(Responder(SearchProj1, worklog));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc));
        signal.DateStarted.Kind.Should().Be(DateTimeKind.Utc);
        signal.DateEnded!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task FetchAsync_ParsesColonlessRfc822Offset()
    {
        // Jira Cloud v3 returns a colonless offset ("+0000") that the built-in ISO parser rejects.
        const string search = """
        {
          "issues": [
            { "id": "1001", "key": "PROJ-1", "fields": {
              "summary": "Fix login",
              "issuetype": { "name": "Bug" },
              "status": { "name": "In Progress" },
              "project": { "key": "PROJ", "name": "Project" },
              "updated": "2026-07-01T12:34:56.789+0000"
            } }
          ],
          "isLast": true
        }
        """;
        const string worklog = """
        {
          "worklogs": [
            { "id": "100", "author": { "accountId": "acc-1" }, "started": "2026-07-01T09:00:00.000+0000", "timeSpentSeconds": 3600 }
          ],
          "total": 1
        }
        """;

        var connector = BuildConnector(Responder(search, worklog));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FetchAsync_ProducesStatusTransitionSignalFromChangelog()
    {
        const string changelog = """
        {
          "issueChangeLogs": [
            {
              "issueId": "1001",
              "changeHistories": [
                {
                  "id": "5000",
                  "author": { "accountId": "acc-1" },
                  "created": "2026-07-01T11:00:00.000+00:00",
                  "items": [ { "field": "status", "fieldId": "status", "from": "1", "fromString": "To Do", "to": "3", "toString": "In Progress" } ]
                }
              ]
            }
          ]
        }
        """;

        var connector = BuildConnector(Responder(SearchProj1, EmptyWorklogs, EmptyComments, changelog));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.ExternalId.Should().Be("PROJ-1:changelog:5000:0");
        signal.Kind.Should().Be(ActivityKind.StatusTransition);
    }

    [Fact]
    public async Task FetchAsync_ParsesEpochMillisChangelogCreated()
    {
        // changelog/bulkfetch returns "created" as epoch milliseconds (a JSON number), not an ISO string.
        const string changelog = """
        {
          "issueChangeLogs": [
            {
              "issueId": "1001",
              "changeHistories": [
                {
                  "id": "5000",
                  "author": { "accountId": "acc-1" },
                  "created": 1782903600000,
                  "items": [ { "field": "status", "fieldId": "status", "from": "1", "fromString": "To Do", "to": "3", "toString": "In Progress" } ]
                }
              ]
            }
          ]
        }
        """;

        var connector = BuildConnector(Responder(SearchProj1, EmptyWorklogs, EmptyComments, changelog));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var signal = result.Value.Should().ContainSingle().Which;
        signal.ExternalId.Should().Be("PROJ-1:changelog:5000:0");
        signal.Kind.Should().Be(ActivityKind.StatusTransition);
    }

    [Fact]
    public async Task FetchAsync_ExcludesEventsFromOtherAuthors()
    {
        const string worklog = """
        {
          "worklogs": [
            { "id": "100", "author": { "accountId": "someone-else" }, "started": "2026-07-01T09:00:00.000+00:00", "timeSpentSeconds": 3600 }
          ],
          "total": 1
        }
        """;

        var connector = BuildConnector(Responder(SearchProj1, worklog));

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
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
        var connector = BuildConnector(_ =>
        {
            var response = StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, "{}");
            response.Headers.Add("Retry-After", "30");
            return response;
        });

        var result = await connector.FetchAsync("user-1", Container, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_WhenMyselfSucceeds()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.OK, Myself));

        var result = await connector.ValidateAsync("user-1", CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsUnauthorized_WhenMyselfRejected()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.Forbidden, "{}"));

        var result = await connector.ValidateAsync("user-1", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> Responder(string search, string worklog, string comments = EmptyComments, string changelog = EmptyChangelog)
    {
        return request =>
        {
            request.Headers.Authorization?.Scheme.Should().Be("Bearer");
            request.Headers.Authorization?.Parameter.Should().Be("access-token");

            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/myself"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, Myself);
            }

            if (uri.Contains("/search/jql"))
            {
                request.Method.Should().Be(HttpMethod.Post);
                var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                body.Should().Contain("\"maxResults\":5000");
                body.Should().Contain("\"fields\":[");
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, search);
            }

            if (uri.Contains("changelog/bulkfetch"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, changelog);
            }

            if (uri.Contains("/worklog"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, worklog);
            }

            if (uri.Contains("/comment"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, comments);
            }

            return StubHttpMessageHandler.Json(HttpStatusCode.OK, "{}");
        };
    }

    private static JiraSignalDetail Detail(ActivitySignal signal)
    {
        return signal.Detail.Should().BeOfType<JiraSignalDetail>().Which;
    }

    private static JiraConnector BuildConnector(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.atlassian.com/") };
        var options = Options.Create(new JiraOptions
        {
            ApiBaseUrl = "https://api.atlassian.com",
            AuthorizeEndpoint = "https://auth.atlassian.com/authorize",
            TokenEndpoint = "https://auth.atlassian.com/oauth/token",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = "read:jira-work read:jira-user offline_access",
            PageSize = 50,
            MaxPages = 20,
            MaxConcurrency = 8,
        });
        var session = new JiraSession(httpClient, options, NullLogger<JiraSession>.Instance, "access-token", "cloud-1", SiteUrl);
        return new JiraConnector(new StubJiraSessionFactory(session), options);
    }
}
