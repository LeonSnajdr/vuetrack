# Consuming, mapping, and testing result types

A result type earns its keep at the call site. The value of naming every outcome
is lost if a caller ignores cases or treats them all the same — so consume a
result with a `switch` expression that has one arm per case.

## Switch over every case

```csharp
private static string Describe(ActivityFetchResult result) => result switch
{
    ActivityFetchSuccess success => $"success:{success.Signals.Count}",
    ActivityFetchAuthFailed authFailed => $"auth:{authFailed.Reason}",
    ActivityFetchRateLimited rateLimited => $"rate:{rateLimited.RetryAfter.TotalSeconds:0}",
    ActivityFetchConnectorError error => $"error:{error.Message}",
    _ => throw new InvalidOperationException(),
};
```

Why a `switch` expression rather than `if (result.IsSuccess)`:

- **Each arm gets the case's own type**, so the case-specific data
  (`success.Signals`, `rateLimited.RetryAfter`) is available and type-checked —
  no casting, no nullable payload that's only sometimes set.
- **It reads as "one outcome, one response"**, which is the contract the result
  type is expressing.
- **The compiler helps.** When you add a case to the family later, arms that
  don't handle it become visible — the analyzer warns that the switch is no
  longer exhaustive.

Keep the discard arm (`_ =>`) as a guard for "impossible" states. Make it loud —
`throw`, or a 500 in a controller — never a silent default that swallows a new
case someone forgot to handle.

When an arm needs no data from the case, match the type alone:
`ValidationValid => "valid"`. When it needs data, bind it: `ValidationInvalid
invalid => invalid.Errors.Count`.

## Mapping a result to an HTTP response

Controllers are where a service's result becomes a status code. Each case maps
to the response that fits its meaning — this is the pattern-match doing exactly
the job `HandleFailure`-style helpers do in flag-based result libraries, but with
each case's data in hand:

```csharp
var result = await ConnectionService.FetchRecommendationsAsync(userId, fromDate, toDate, cancellationToken);

return result switch
{
    null => Conflict(new { errors = new[] { "Jira is not connected." } }),
    ActivityFetchSuccess success => Ok(success.Signals),
    ActivityFetchAuthFailed authFailed => Unauthorized(new { errors = new[] { authFailed.Reason } }),
    ActivityFetchRateLimited rateLimited => StatusCode(429, new { retryAfterSeconds = rateLimited.RetryAfter.TotalSeconds }),
    ActivityFetchConnectorError error => StatusCode(502, new { errors = new[] { error.Message } }),
    _ => StatusCode(500),
};
```

Notes:

- The mapping lives in the **controller**, not the service. The service returns
  the domain outcome; the controller decides how to express it over HTTP. This
  keeps the service reusable by callers that aren't HTTP (jobs, other services).
- Map each case to the status that matches its *meaning*: validation → 400,
  auth → 401/403, not-found → 404, conflict → 409, rate-limited → 429, upstream
  failure → 502. Don't collapse distinct outcomes into one status just because
  it's less code — the distinction is why the result type exists.
- A result **cross the API boundary** as a `Contract`, not as the raw result
  record. Above, the success payload (`success.Signals`) and shaped error
  objects are what serialize; the `ActivityFetchResult` cases themselves stay internal.

### `null` for an outcome outside the family

`FetchRecommendationsAsync` returns `ActivityFetchResult?` and uses `null` for "the user
isn't connected" — a *precondition* that sits outside the fetch's own outcomes.
That's a deliberate, narrow use of `null`: a state orthogonal to the family,
handled as its own arm. Prefer an explicit case inside the family when the state
is genuinely one of the operation's outcomes; reach for a nullable result only
when the extra state is a precondition the operation never really began under.

## Testing each case

Because cases are records, you construct them directly and assert against them —
record equality means no custom setup. Cover the mapping so every case is
exercised at least once:

```csharp
[Fact]
public void ActivityFetchResult_MatchesEachCase()
{
    Assert.Equal("success:2", Describe(new ActivityFetchSuccess([Signal(), Signal()])));
    Assert.Equal("auth:nope", Describe(new ActivityFetchAuthFailed("nope")));
    Assert.Equal("rate:30", Describe(new ActivityFetchRateLimited(TimeSpan.FromSeconds(30))));
    Assert.Equal("error:boom", Describe(new ActivityFetchConnectorError("boom")));
}
```

See
[`Vuetrack.Connectors.Jira.Tests/ResultTypeTests.cs`](../../../../Vuetrack.Connectors.Jira.Tests/ResultTypeTests.cs)
for the full example, including the parallel test for `ValidationOutcome`.

The rule of thumb: **one assertion per case**. If you add a case to the family
and don't add a test line, that's the same gap the non-exhaustive `switch`
warning is pointing at — close both together.
