using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Tests.Fakes;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Details;

public class DetailServiceTests
{
    [Fact]
    public async Task GetAsync_SourceReturnsFields_GroupsThemUnderItsKey()
    {
        var jira = new FakeConnector(IntegrationKey.Jira, NoSignals, (_, _) => Fields(
        [
            new TextDetailField { Label = DetailFieldLabel.Title, Value = "Fix the thing" },
        ]));

        var service = CreateService(jira);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Groups.Should().ContainSingle();
        result.Value.Groups[0].Key.Should().Be(IntegrationKey.Jira);
        result.Value.Groups[0].Fields.Should().ContainSingle(f => f.Label == DetailFieldLabel.Title);
    }

    [Fact]
    public async Task GetAsync_SourceNotConnected_OmitsItsGroup()
    {
        var jira = new FakeConnector(IntegrationKey.Jira, NoSignals, (_, _) => FailFields(IntegrationError.NotConnected));

        var service = CreateService(jira);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_OneSourceFails_KeepsTheOtherGroup()
    {
        var jira = new FakeConnector(IntegrationKey.Jira, NoSignals, (_, _) => Fields(
        [
            new TextDetailField { Label = DetailFieldLabel.Title, Value = "Fix the thing" },
        ]));
        var github = new FakeConnector(IntegrationKey.Github, NoSignals, (_, _) => FailFields(Error.Failure()));

        var service = CreateService(jira, github);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        jira.DetailCount.Should().Be(1);
        github.DetailCount.Should().Be(1);
        result.Value.Groups.Should().ContainSingle(g => g.Key == IntegrationKey.Jira);
    }

    [Fact]
    public async Task GetAsync_SourceReturnsNoFields_OmitsItsGroup()
    {
        var jira = new FakeConnector(IntegrationKey.Jira, NoSignals, (_, _) => Fields([]));

        var service = CreateService(jira);

        var result = await service.GetAsync(Query(), "user-1", CancellationToken.None);

        jira.DetailCount.Should().Be(1);
        result.Value.Groups.Should().BeEmpty();
    }

    private static DetailService CreateService(params IConnector[] connectors)
    {
        var registry = new FakeIntegrationRegistry();
        foreach (var connector in connectors)
        {
            registry.Add(connector);
        }

        return new DetailService(registry, NullLogger<DetailService>.Instance);
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
}
