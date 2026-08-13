using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.Connection;
using Vuetrack.Api.Features.Integrations.Jira.Connection;

namespace Vuetrack.Api.Tests.Fakes;

public class StubConnectionContextFactory<TContext>(TContext? context) : IConnectionContextFactory<TContext>
    where TContext : class
{
    public Task<TContext?> CreateAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(context);

    public Task EvictAsync(string userId, CancellationToken cancellationToken) => Task.CompletedTask;
}

// The closed subclasses exist only because each provider declares its own marker interface for DI.
public sealed class StubJiraConnectionContextFactory(JiraConnectionContext? context)
    : StubConnectionContextFactory<JiraConnectionContext>(context), IJiraConnectionContextFactory;

public sealed class StubGithubConnectionContextFactory(GithubConnectionContext? context)
    : StubConnectionContextFactory<GithubConnectionContext>(context), IGithubConnectionContextFactory;
