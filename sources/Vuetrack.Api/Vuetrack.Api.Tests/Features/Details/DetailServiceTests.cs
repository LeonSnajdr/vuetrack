using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Tests.Fakes;
using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Details;

public class DetailServiceTests
{
    [Fact]
    public async Task GetAsync_ConnectedConnector_ReturnsFieldsGroupedUnderItsKey()
    {
        var jira = new FakeConnector(Descriptor(ConnectorKey.Jira), NoSignals, (_, _) => Fields(
        [
            new TextDetailField { Label = DetailFieldLabel.Title, Value = "Fix the thing" },
        ]));

        var registry = new FakeConnectorRegistry();
        registry.Add(jira);

        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)]);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        result.Groups.Should().ContainSingle();
        result.Groups[0].ConnectorKey.Should().Be(ConnectorKey.Jira);
        result.Groups[0].Fields.Should().ContainSingle(f => f.Label == DetailFieldLabel.Title);
    }

    [Fact]
    public async Task GetAsync_ConnectorNotConnected_IsNeverAskedForDetails()
    {
        var jira = new FakeConnector(Descriptor(ConnectorKey.Jira), NoSignals, (_, _) => Fields(
        [
            new TextDetailField { Label = DetailFieldLabel.Title, Value = "Fix the thing" },
        ]));

        var registry = new FakeConnectorRegistry();
        registry.Add(jira);

        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, false)]);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        jira.DetailCount.Should().Be(0);
        result.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_OnlyOneConnectorConnected_AsksConnectedOneOnly()
    {
        var jira = new FakeConnector(Descriptor(ConnectorKey.Jira), NoSignals, (_, _) => Fields(
        [
            new TextDetailField { Label = DetailFieldLabel.Title, Value = "Fix the thing" },
        ]));
        var github = new FakeConnector(Descriptor(ConnectorKey.Github), NoSignals, (_, _) => Fields(
        [
            new TextDetailField { Label = DetailFieldLabel.Status, Value = "open" },
        ]));

        var registry = new FakeConnectorRegistry();
        registry.Add(jira);
        registry.Add(github);

        var initializers = new IConnectorContextInitializer[]
        {
            new FakeConnectorContextInitializer(ConnectorKey.Jira, true),
            new FakeConnectorContextInitializer(ConnectorKey.Github, false),
        };

        var service = CreateService(registry, initializers);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        jira.DetailCount.Should().Be(1);
        github.DetailCount.Should().Be(0);
        result.Groups.Should().ContainSingle(g => g.ConnectorKey == ConnectorKey.Jira);
    }

    [Fact]
    public async Task GetAsync_ConnectorFails_IsSwallowedAndOmitsItsGroup()
    {
        var jira = new FakeConnector(Descriptor(ConnectorKey.Jira), NoSignals, (_, _) => FailFields(Error.Failure()));

        var registry = new FakeConnectorRegistry();
        registry.Add(jira);

        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)]);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        jira.DetailCount.Should().Be(1);
        result.Groups.Should().BeEmpty();
    }

    private static DetailService CreateService(FakeConnectorRegistry registry, IEnumerable<IConnectorContextInitializer> initializers)
    {
        var resolver = new ConnectorResolver(registry, initializers);
        return new DetailService(resolver, NullLogger<DetailService>.Instance);
    }

    private static Task<ErrorOr<IReadOnlyList<ActivitySignal>>> NoSignals(ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        IReadOnlyList<ActivitySignal> empty = [];
        return Task.FromResult(empty.ToErrorOr());
    }

    private static Task<ErrorOr<IReadOnlyList<DetailField>>> Fields(IReadOnlyList<DetailField> fields) =>
        Task.FromResult<ErrorOr<IReadOnlyList<DetailField>>>(fields.ToErrorOr());

    private static Task<ErrorOr<IReadOnlyList<DetailField>>> FailFields(Error error) =>
        Task.FromResult<ErrorOr<IReadOnlyList<DetailField>>>(error);

    private static DetailQuery Query() => new() { TaskId = "J-1" };

    private static ConnectorDescriptor Descriptor(ConnectorKey key) => new()
    {
        Key = key,
        Capabilities = [],
    };
}
