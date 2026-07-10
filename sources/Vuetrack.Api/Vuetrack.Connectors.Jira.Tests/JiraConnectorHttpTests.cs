using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira;
using Vuetrack.Connectors.Jira.Activity;
using Vuetrack.Connectors.Jira.Connection;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraConnectorHttpTests
{
    private const string SiteUrl = "https://acme.atlassian.net";

    private static readonly ActivityFetchContainer Container = new()
    {
        From = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
        To = new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero),
    };

    private static JiraConnector BuildConnector(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new JiraOptions());
        var accessor = new JiraConnectionAccessor
        {
            Current = new JiraConnectionContainer
            {
                UserId = "user-1",
                AccessToken = "access-token",
                CloudId = "cloud-1",
                SiteUrl = SiteUrl,
            },
        };
        var client = new JiraApiClient(httpClient, accessor, options, NullLogger<JiraApiClient>.Instance);
        return new JiraConnector(client, accessor);
    }

    [Fact]
    public async Task FetchAsync_ReturnsSuccessWithSignals_OnHappyPath()
    {
        var connector = BuildConnector(Respond);

        var result = await connector.FetchAsync(Container, CancellationToken.None);

        var success = result.Should().BeOfType<ActivityFetchSuccess>().Which;

        var signal = success.Signals.Should().ContainSingle().Which;
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.Link.Should().Be("https://acme.atlassian.net/browse/PROJ-1");
        signal.Description.Should().Be("worked on the fix");

        static HttpResponseMessage Respond(HttpRequestMessage request)
        {
            // The client (not a delegating handler) attaches the ambient access token.
            request.Headers.Authorization?.Scheme.Should().Be("Bearer");
            request.Headers.Authorization?.Parameter.Should().Be("access-token");

            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/myself"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "accountId": "acc-1" }""");
            }

            if (uri.Contains("/search/jql"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, """
                {
                  "issues": [
                    { "key": "PROJ-1", "fields": {
                      "summary": "Fix login",
                      "issuetype": { "name": "Bug" },
                      "status": { "name": "In Progress" },
                      "project": { "key": "PROJ" },
                      "updated": "2026-07-01T15:00:00.000+00:00"
                    } }
                  ],
                  "isLast": true
                }
                """);
            }

            return StubHttpMessageHandler.Json(HttpStatusCode.OK, """
            {
              "worklogs": [
                {
                  "id": "100",
                  "author": { "accountId": "acc-1" },
                  "started": "2026-07-01T09:00:00.000+00:00",
                  "timeSpentSeconds": 3600,
                  "comment": { "type": "doc", "content": [
                    { "type": "paragraph", "content": [ { "type": "text", "text": "worked on the fix" } ] }
                  ] }
                }
              ]
            }
            """);
        }
    }

    [Fact]
    public async Task FetchAsync_MergesWorklogAndUnrelatedIssueFallback()
    {
        var connector = BuildConnector(Respond);

        var result = await connector.FetchAsync(Container, CancellationToken.None);

        var success = result.Should().BeOfType<ActivityFetchSuccess>().Which;

        success.Signals.Should().HaveCount(2);
        success.Signals.Should().Contain(s => s.ExternalId == "PROJ-1:worklog:100");
        success.Signals.Should().Contain(s => s.ExternalId == "PROJ-2:issue");

        static HttpResponseMessage Respond(HttpRequestMessage request)
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("/myself"))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "accountId": "acc-1" }""");
            }

            // The worklog search and the issue-activity search both hit /search/jql; tell them apart by JQL.
            if (uri.Contains("/search/jql"))
            {
                var issueKey = uri.Contains("worklogAuthor") ? "PROJ-1" : "PROJ-2";
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, $$"""
                {
                  "issues": [
                    { "key": "{{issueKey}}", "fields": {
                      "summary": "Some work",
                      "updated": "2026-07-01T15:00:00.000+00:00"
                    } }
                  ],
                  "isLast": true
                }
                """);
            }

            return StubHttpMessageHandler.Json(HttpStatusCode.OK, """
            {
              "worklogs": [
                {
                  "id": "100",
                  "author": { "accountId": "acc-1" },
                  "started": "2026-07-01T09:00:00.000+00:00",
                  "timeSpentSeconds": 3600
                }
              ]
            }
            """);
        }
    }

    [Fact]
    public async Task FetchAsync_ReturnsAuthFailed_On401()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{}"));

        var result = await connector.FetchAsync(Container, CancellationToken.None);

        result.Should().BeOfType<ActivityFetchAuthFailed>();
    }

    [Fact]
    public async Task FetchAsync_ReturnsRateLimited_On429WithRetryAfter()
    {
        var connector = BuildConnector(_ =>
        {
            var response = StubHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, "{}");
            response.Headers.Add("Retry-After", "30");
            return response;
        });

        var result = await connector.FetchAsync(Container, CancellationToken.None);

        var rateLimited = result.Should().BeOfType<ActivityFetchRateLimited>().Which;

        rateLimited.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenMyselfSucceeds()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.OK, """{ "accountId": "acc-1" }"""));

        var result = await connector.ValidateAsync(CancellationToken.None);

        result.Should().BeOfType<ConnectorValidationValid>();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenMyselfRejected()
    {
        var connector = BuildConnector(_ => StubHttpMessageHandler.Json(HttpStatusCode.Forbidden, "{}"));

        var result = await connector.ValidateAsync(CancellationToken.None);

        result.Should().BeOfType<ConnectorValidationInvalid>();
    }
}
