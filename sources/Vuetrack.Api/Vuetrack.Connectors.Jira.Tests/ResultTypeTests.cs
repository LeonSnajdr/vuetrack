using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class ResultTypeTests
{
    [Fact]
    public void FetchResult_MatchesEachCase()
    {
        Assert.Equal("success:2", Describe(new Success([Signal(), Signal()])));
        Assert.Equal("auth:nope", Describe(new AuthFailed("nope")));
        Assert.Equal("rate:30", Describe(new RateLimited(TimeSpan.FromSeconds(30))));
        Assert.Equal("error:boom", Describe(new ConnectorError("boom")));
    }

    [Fact]
    public void ValidationOutcome_MatchesEachCase()
    {
        Assert.Equal("valid", Describe(new Valid()));
        Assert.Equal("invalid:2", Describe(new Invalid(["a", "b"])));
    }

    private static string Describe(FetchResult result) => result switch
    {
        Success success => $"success:{success.Signals.Count}",
        AuthFailed authFailed => $"auth:{authFailed.Reason}",
        RateLimited rateLimited => $"rate:{rateLimited.RetryAfter.TotalSeconds:0}",
        ConnectorError error => $"error:{error.Message}",
        _ => throw new InvalidOperationException(),
    };

    private static string Describe(ValidationOutcome outcome) => outcome switch
    {
        Valid => "valid",
        Invalid invalid => $"invalid:{invalid.Errors.Count}",
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
