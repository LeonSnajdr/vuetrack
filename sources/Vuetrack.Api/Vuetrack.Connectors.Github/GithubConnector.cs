using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Github.Activity;

namespace Vuetrack.Connectors.Github;

[InjectAs(typeof(IConnector))]
public class GithubConnector(IGithubApiClient client) : IConnector
{
    public const ConnectorKey Key = ConnectorKey.Github;

    private IGithubApiClient Client { get; } = client;

    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Key = Key,
        DisplayName = "GitHub",
        Capabilities = ConnectorCapabilities.IssueActivity | ConnectorCapabilities.OAuth,
    };

    public async Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var login = await Client.GetAuthenticatedLoginAsync(cancellationToken);

            if (string.IsNullOrEmpty(login))
            {
                return Error.Validation(description: "GitHub did not return a user for these credentials.");
            }

            return Result.Success;
        }
        catch (GithubApiException ex)
        {
            return MapError(ex);
        }
    }

    public async Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        try
        {
            var login = await Client.GetAuthenticatedLoginAsync(cancellationToken);
            if (string.IsNullOrEmpty(login))
            {
                return Error.Unauthorized();
            }

            var commits = await Client.SearchCommitsAsync(login, container.From, container.To, cancellationToken);

            // Keyed by ExternalId so overlapping fetch windows collapse deterministically before the engine.
            var signals = new Dictionary<string, ActivitySignal>(StringComparer.Ordinal);

            foreach (var commit in commits)
            {
                if (string.IsNullOrEmpty(commit.Sha) || commit.Repository?.FullName is not { Length: > 0 })
                {
                    continue;
                }

                var signal = GithubActivityMapper.ToCommitSignal(commit);
                signals[signal.ExternalId] = signal;
            }

            IReadOnlyList<ActivitySignal> result = signals.Values.ToList();
            var errorOr = result.ToErrorOr();
            return errorOr;
        }
        catch (GithubApiException ex)
        {
            return MapError(ex);
        }
    }

    public Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(DetailQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<DetailField> empty = [];
        return Task.FromResult(empty.ToErrorOr());
    }

    private static Error MapError(GithubApiException ex)
    {
        return ex.Kind switch
        {
            GithubApiErrorKind.Auth => Error.Unauthorized(),
            _ => Error.Failure(),
        };
    }
}
