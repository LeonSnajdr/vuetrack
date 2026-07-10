namespace Vuetrack.Connectors.Abstractions;

public abstract record FetchOutcome;

public sealed record FetchSuccess(IReadOnlyList<ActivitySignal> Signals) : FetchOutcome;

public sealed record FetchAuthFailed(string Reason) : FetchOutcome;

public sealed record FetchRateLimited(TimeSpan RetryAfter) : FetchOutcome;

public sealed record FetchConnectorError(string Message) : FetchOutcome;

public sealed record FetchNotConnected : FetchOutcome;
