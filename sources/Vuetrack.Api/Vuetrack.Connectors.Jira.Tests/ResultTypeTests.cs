using AwesomeAssertions;
using Vuetrack.Api.Features.Connectors.Jira;
using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class ResultTypeTests
{
    [Fact]
    public void FetchResult_MatchesEachCase()
    {
        Describe(new FetchSuccess([Signal(), Signal()])).Should().Be("success:2");
        Describe(new FetchAuthFailed("nope")).Should().Be("auth:nope");
        Describe(new FetchRateLimited(TimeSpan.FromSeconds(30))).Should().Be("rate:30");
        Describe(new FetchConnectorError("boom")).Should().Be("error:boom");
        Describe(new FetchNotConnected()).Should().Be("notConnected");
    }

    [Fact]
    public void ValidationOutcome_MatchesEachCase()
    {
        Describe(new ValidationValid()).Should().Be("valid");
        Describe(new ValidationInvalid(["a", "b"])).Should().Be("invalid:2");
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

    private static string Describe(FetchResult result) => result switch
    {
        FetchSuccess success => $"success:{success.Signals.Count}",
        FetchAuthFailed authFailed => $"auth:{authFailed.Reason}",
        FetchRateLimited rateLimited => $"rate:{rateLimited.RetryAfter.TotalSeconds:0}",
        FetchConnectorError error => $"error:{error.Message}",
        FetchNotConnected => "notConnected",
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

    private static string Describe(ValidationOutcome outcome) => outcome switch
    {
        ValidationValid => "valid",
        ValidationInvalid invalid => $"invalid:{invalid.Errors.Count}",
        _ => throw new InvalidOperationException(),
    };

    private static ActivitySignal Signal() => new()
    {
        ConnectorKey = "jira",
        ExternalId = "PROJ-1:issue",
        Title = "PROJ-1",
        Start = DateTimeOffset.UnixEpoch,
    };
}
