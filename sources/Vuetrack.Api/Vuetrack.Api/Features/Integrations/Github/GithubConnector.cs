using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Github.Api;
using Vuetrack.Api.Features.Integrations.Github.Connection;
using Vuetrack.Api.Features.Integrations.Github.Internal;

namespace Vuetrack.Api.Features.Integrations.Github;

[InjectAs(typeof(IConnector))]
public class GithubConnector(IGithubApiClient client, IGithubConnectionContextFactory contextFactory) : IConnector
{
    private IGithubApiClient Client { get; } = client;

    private IGithubConnectionContextFactory ContextFactory { get; } = contextFactory;

    public IntegrationKey Key => IntegrationKey.Github;

    public async Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken)
    {
        var context = await ContextFactory.CreateAsync(userId, cancellationToken);
        if (context is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var login = await Client.GetAuthenticatedLoginAsync(context, cancellationToken);

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

    public async Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(string userId, ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        var context = await ContextFactory.CreateAsync(userId, cancellationToken);
        if (context is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var login = await Client.GetAuthenticatedLoginAsync(context, cancellationToken);
            if (string.IsNullOrEmpty(login))
            {
                return Error.Unauthorized();
            }

            var commits = await Client.SearchCommitsAsync(context, login, container.From, container.To, cancellationToken);

            var signals = new Dictionary<string, ActivitySignal>(StringComparer.Ordinal);

            foreach (var commit in commits)
            {
                if (string.IsNullOrEmpty(commit.Sha) || commit.Repository?.FullName is not { Length: > 0 })
                {
                    continue;
                }

                var signal = commit.ToCommitSignal();
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

    public Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(string userId, DetailQuery query, CancellationToken cancellationToken)
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
