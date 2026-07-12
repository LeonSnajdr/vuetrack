using AwesomeAssertions;
using Vuetrack.Api.Features.Connectors.Jira;
using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class ResultTypeTests
{
    [Fact]
    public void ActivityFetchResult_MatchesEachCase()
    {
        Describe(new ActivityFetchSuccess([Signal(), Signal()])).Should().Be("success:2");
        Describe(new ActivityFetchAuthFailed("nope")).Should().Be("auth:nope");
        Describe(new ActivityFetchRateLimited(TimeSpan.FromSeconds(30))).Should().Be("rate:30");
        Describe(new ActivityFetchConnectorError("boom")).Should().Be("error:boom");
        Describe(new ActivityFetchNotConnected()).Should().Be("notConnected");
    }

    [Fact]
    public void ValidationResult_MatchesEachCase()
    {
        Describe(new ConnectorValidationValid()).Should().Be("valid");
        Describe(new ConnectorValidationInvalid(["a", "b"])).Should().Be("invalid:2");
    }

    [Fact]
    public void JiraConnectResult_MatchesEachCase()
    {
        Describe(new JiraConnectSuccess("https://site")).Should().Be("success:https://site");
        Describe(new JiraConnectNoSite()).Should().Be("noSite");
        Describe(new JiraConnectValidationFailed(["a", "b"])).Should().Be("invalid:2");
        Describe(new JiraConnectAuthFailed("nope")).Should().Be("auth:nope");
        Describe(new JiraConnectRateLimited(TimeSpan.FromSeconds(30))).Should().Be("rate:30");
        Describe(new JiraConnectError("boom")).Should().Be("error:boom");
    }

    private static string Describe(ActivityFetchResult result) => result switch
    {
        ActivityFetchSuccess success => $"success:{success.Signals.Count}",
        ActivityFetchAuthFailed authFailed => $"auth:{authFailed.Reason}",
        ActivityFetchRateLimited rateLimited => $"rate:{rateLimited.RetryAfter.TotalSeconds:0}",
        ActivityFetchConnectorError error => $"error:{error.Message}",
        ActivityFetchNotConnected => "notConnected",
        _ => throw new InvalidOperationException(),
    };

    private static string Describe(JiraConnectResult result) => result switch
    {
        JiraConnectSuccess success => $"success:{success.SiteUrl}",
        JiraConnectNoSite => "noSite",
        JiraConnectValidationFailed invalid => $"invalid:{invalid.Errors.Count}",
        JiraConnectAuthFailed authFailed => $"auth:{authFailed.Reason}",
        JiraConnectRateLimited rateLimited => $"rate:{rateLimited.RetryAfter.TotalSeconds:0}",
        JiraConnectError error => $"error:{error.Message}",
        _ => throw new InvalidOperationException(),
    };

    private static string Describe(ConnectorValidationResult result) => result switch
    {
        ConnectorValidationValid => "valid",
        ConnectorValidationInvalid invalid => $"invalid:{invalid.Errors.Count}",
        _ => throw new InvalidOperationException(),
    };

    private static ActivitySignal Signal() => new()
    {
        ConnectorKey = ConnectorKey.Jira,
        ExternalId = "PROJ-1:issue",
        Title = "PROJ-1",
        Start = DateTime.UnixEpoch,
    };
}
