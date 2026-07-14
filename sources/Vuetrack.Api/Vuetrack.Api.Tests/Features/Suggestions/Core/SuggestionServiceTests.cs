using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Suggestions.Core;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Core.Services;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Api.Tests.Fakes;
using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions.Core;

public class SuggestionServiceTests
{
    private static readonly DateTime BaseDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime From = BaseDate;

    private static readonly DateTime To = BaseDate.AddDays(1);

    [Fact]
    public async Task GenerateAsync_ConnectorReturnsMultipleSignals_AggregatesAndPersistsAllSuggestions()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1 work", At(9, 0), At(9, 10)),
            Signal(ConnectorKey.Jira, "J-2:worklog:1", "J-2 work", At(11, 0), At(11, 10)),
        ])));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, true) };

        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.GeneratedCount.Should().Be(2);
        var outcome = result.ConnectorOutcomes.Should().ContainSingle().Subject;
        outcome.Status.Should().Be("Success");
        outcome.SignalCount.Should().Be(2);
        repository.Items.Should().HaveCount(2);
    }

    public enum OutcomeScenario
    {
        Success,
        AuthFailed,
        ConnectorError,
        Throws,
        NotConnected,
    }

    [Theory]
    [InlineData(OutcomeScenario.Success, "Success")]
    [InlineData(OutcomeScenario.AuthFailed, "AuthFailed")]
    [InlineData(OutcomeScenario.ConnectorError, "Error")]
    [InlineData(OutcomeScenario.Throws, "Error")]
    [InlineData(OutcomeScenario.NotConnected, "NotConnected")]
    public async Task GenerateAsync_ConnectorOutcome_IsRecordedWithoutThrowing(OutcomeScenario scenario, string expectedStatus)
    {
        var connected = scenario != OutcomeScenario.NotConnected;

        Func<ActivityFetchContainer, CancellationToken, Task<ErrorOr<IReadOnlyList<ActivitySignal>>>> fetch = (_, _) => scenario switch
        {
            OutcomeScenario.Success => Signals([Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1 work", At(9, 0), At(9, 10))]),
            OutcomeScenario.AuthFailed => Fail(Error.Unauthorized()),
            OutcomeScenario.ConnectorError => Fail(Error.Failure()),
            OutcomeScenario.Throws => throw new InvalidOperationException("boom"),
            OutcomeScenario.NotConnected => throw new InvalidOperationException("should never be called"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };

        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), fetch));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, connected) };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        var outcome = result.ConnectorOutcomes.Should().ContainSingle().Subject;
        outcome.ConnectorKey.Should().Be(ConnectorKey.Jira);
        outcome.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task GenerateAsync_SecondRunOverSameRange_InsertsNothingNewAndPreservesEditedStatus()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1 work", At(9, 0), At(9, 10)),
            Signal(ConnectorKey.Jira, "J-2:worklog:1", "J-2 work", At(12, 0), At(12, 10)),
        ])));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, true) };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var first = await service.GenerateAsync("user-1", Request(), CancellationToken.None);
        first.GeneratedCount.Should().Be(2);
        repository.Items.Should().HaveCount(2);

        repository.Items[0].Status = SuggestionStatus.Edited;

        var second = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        second.GeneratedCount.Should().Be(0);
        repository.Items.Should().HaveCount(2);
        repository.Items[0].Status.Should().Be(SuggestionStatus.Edited);
        repository.Items[1].Status.Should().Be(SuggestionStatus.Pending);
    }

    [Fact]
    public async Task ListAsync_NeverReturnsAnotherUsersSuggestions()
    {
        var repository = new FakeSuggestionRepository();
        await repository.Save(BuildModel("user-a", "Mine", At(9, 0), At(9, 30)));
        await repository.Save(BuildModel("user-b", "NotMine", At(9, 0), At(9, 30)));

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.ListAsync("user-a", From, To);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Mine");
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Mine", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-b", model.Id, UpdateContract("Changed", At(9, 0), At(10, 0)), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DismissAsync_AnotherUsersSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Mine", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-b", model.Id, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSuggestion_UpdatesFieldsAndSetsEditedStatus()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Original", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-a", model.Id, UpdateContract("Changed", At(10, 0), At(11, 0)), CancellationToken.None);

        result.IsError.Should().BeFalse();
        var updated = result.Value;
        updated.Title.Should().Be("Changed");
        updated.DateStarted.Should().Be(At(10, 0));
        updated.DateEnded.Should().Be(At(11, 0));
        updated.Status.Should().Be(nameof(SuggestionStatus.Edited));
    }

    [Fact]
    public async Task UpdateAsync_MissingSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-a", "missing-id", UpdateContract("Changed", At(9, 0), At(10, 0)), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DismissAsync_ExistingSuggestion_SetsDismissedStatus()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Original", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-a", model.Id, CancellationToken.None);

        result.IsError.Should().BeFalse();
        repository.Items.Should().ContainSingle(x => x.Status == SuggestionStatus.Dismissed);
    }

    [Fact]
    public async Task DismissAsync_MissingSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-a", "missing-id", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    private static SuggestionService CreateService(FakeConnectorRegistry registry, IEnumerable<IConnectorContextInitializer> initializers, FakeSuggestionRepository repository)
    {
        var engine = new SuggestionEngine(Options.Create(new SuggestionEngineOptions()));
        var resolver = new ConnectorResolver(registry, initializers);
        return new SuggestionService(registry, resolver, repository, engine, NullLogger<SuggestionService>.Instance);
    }

    private static Task<ErrorOr<IReadOnlyList<ActivitySignal>>> Signals(IReadOnlyList<ActivitySignal> signals) =>
        Task.FromResult<ErrorOr<IReadOnlyList<ActivitySignal>>>(signals.ToErrorOr());

    private static Task<ErrorOr<IReadOnlyList<ActivitySignal>>> Fail(Error error) =>
        Task.FromResult<ErrorOr<IReadOnlyList<ActivitySignal>>>(error);

    private static GenerateSuggestionsRequestContract Request() => new() { From = From, To = To };

    private static SuggestionUpdateContract UpdateContract(string title, DateTime start, DateTime end) => new()
    {
        Title = title,
        DateStarted = start,
        DateEnded = end,
    };

    private static SuggestionModel BuildModel(string userId, string title, DateTime start, DateTime end) => new()
    {
        UserId = userId,
        Title = title,
        DateStarted = start,
        DateEnded = end,
        Status = SuggestionStatus.Pending,
        Sources = [],
        Confidence = 0.5,
        DateCreated = start,
        DateUpdated = start,
    };

    private static ConnectorDescriptor Descriptor(ConnectorKey key) => new()
    {
        Key = key,
        DisplayName = key.ToString(),
        Capabilities = ConnectorCapabilities.None,
    };

    private static DateTime At(int hour, int minute) => BaseDate + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);

    private static ActivitySignal Signal(ConnectorKey connectorKey, string externalId, string title, DateTime start, DateTime? end = null) => new()
    {
        ConnectorKey = connectorKey,
        ExternalId = externalId,
        Title = title,
        DateStarted = start,
        DateEnded = end,
    };
}
