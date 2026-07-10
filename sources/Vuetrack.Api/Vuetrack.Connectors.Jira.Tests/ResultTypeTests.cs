using Vuetrack.Api.Features.Connectors.Jira;
using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class ResultTypeTests
{
    [Fact]
    public void FetchResult_MatchesEachCase()
    {
        Assert.Equal("success:2", Describe(new FetchSuccess([Signal(), Signal()])));
        Assert.Equal("auth:nope", Describe(new FetchAuthFailed("nope")));
        Assert.Equal("rate:30", Describe(new FetchRateLimited(TimeSpan.FromSeconds(30))));
        Assert.Equal("error:boom", Describe(new FetchConnectorError("boom")));
        Assert.Equal("notConnected", Describe(new FetchNotConnected()));
    }

    [Fact]
    public void ValidationOutcome_MatchesEachCase()
    {
        Assert.Equal("valid", Describe(new ValidationValid()));
        Assert.Equal("invalid:2", Describe(new ValidationInvalid(["a", "b"])));
    }

    [Fact]
    public void JiraConnectResult_MatchesEachCase()
    {
        Assert.Equal("success:https://site", Describe(new JiraConnectSuccess("https://site")));
        Assert.Equal("noSite", Describe(new JiraConnectNoSite()));
        Assert.Equal("invalid:2", Describe(new JiraConnectValidationFailed(["a", "b"])));
        Assert.Equal("auth:nope", Describe(new JiraConnectAuthFailed("nope")));
        Assert.Equal("rate:30", Describe(new JiraConnectRateLimited(TimeSpan.FromSeconds(30))));
        Assert.Equal("error:boom", Describe(new JiraConnectError("boom")));
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
