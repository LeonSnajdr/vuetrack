namespace Vuetrack.Connectors.Abstractions;

public abstract record FetchResult;

public sealed record Success(IReadOnlyList<ActivitySignal> Signals) : FetchResult;

public sealed record AuthFailed(string Reason) : FetchResult;

public sealed record RateLimited(TimeSpan RetryAfter) : FetchResult;

public sealed record ConnectorError(string Message) : FetchResult;
