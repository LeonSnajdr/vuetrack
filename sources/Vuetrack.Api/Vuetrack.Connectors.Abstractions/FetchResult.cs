namespace Vuetrack.Connectors.Abstractions;

public abstract record FetchResult;

public sealed record FetchSuccess(IReadOnlyList<ActivitySignal> Signals) : FetchResult;

public sealed record FetchAuthFailed(string Reason) : FetchResult;

public sealed record FetchRateLimited(TimeSpan RetryAfter) : FetchResult;

public sealed record FetchConnectorError(string Message) : FetchResult;
