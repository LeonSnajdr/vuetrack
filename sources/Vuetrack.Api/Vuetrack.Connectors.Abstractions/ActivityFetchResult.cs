namespace Vuetrack.Connectors.Abstractions;

public abstract record ActivityFetchResult;

public sealed record ActivityFetchSuccess(IReadOnlyList<ActivitySignal> Signals) : ActivityFetchResult;

public sealed record ActivityFetchAuthFailed(string Reason) : ActivityFetchResult;

public sealed record ActivityFetchRateLimited(TimeSpan RetryAfter) : ActivityFetchResult;

public sealed record ActivityFetchConnectorError(string Message) : ActivityFetchResult;

public sealed record ActivityFetchNotConnected : ActivityFetchResult;
