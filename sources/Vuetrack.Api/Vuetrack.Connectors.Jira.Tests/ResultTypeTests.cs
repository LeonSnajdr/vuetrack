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
    }

    [Fact]
    public void ValidationOutcome_MatchesEachCase()
    {
        Assert.Equal("valid", Describe(new ValidationValid()));
        Assert.Equal("invalid:2", Describe(new ValidationInvalid(["a", "b"])));
    }

    private static string Describe(FetchResult result) => result switch
    {
        FetchSuccess success => $"success:{success.Signals.Count}",
        FetchAuthFailed authFailed => $"auth:{authFailed.Reason}",
        FetchRateLimited rateLimited => $"rate:{rateLimited.RetryAfter.TotalSeconds:0}",
        FetchConnectorError error => $"error:{error.Message}",
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
