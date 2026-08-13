using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.Api;
using Vuetrack.Api.Features.Integrations.Github.Connection;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Connection;

namespace Vuetrack.Api.Tests.Fakes;

public class StubSessionFactory<TSession>(TSession? session) : IConnectionSessionFactory<TSession>
    where TSession : class
{
    public Task<TSession?> OpenAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(session);

    public Task EvictAsync(string userId, CancellationToken cancellationToken) => Task.CompletedTask;
}

// The closed subclasses exist only because each provider declares its own marker interface for DI.
public sealed class StubJiraSessionFactory(IJiraSession? session)
    : StubSessionFactory<IJiraSession>(session), IJiraSessionFactory;

public sealed class StubGithubSessionFactory(IGithubSession? session)
    : StubSessionFactory<IGithubSession>(session), IGithubSessionFactory;
