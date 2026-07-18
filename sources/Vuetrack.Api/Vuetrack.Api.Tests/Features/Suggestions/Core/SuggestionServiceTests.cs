using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Suggestions.Core;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Core.Services;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Api.Features.TimeEntry.Services;
using Vuetrack.Api.Tests.Fakes;
using Vuetrack.Backends.Abstractions.Contracts;
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
            Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1", At(9, 0), At(9, 10)),
            Signal(ConnectorKey.Jira, "J-2:worklog:1", "J-2", At(11, 0), At(11, 10)),
        ])));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, true) };

        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        repository.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateAsync_ConnectorFails_IsSwallowedAndReturnsEmpty()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Fail(Error.Failure())));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, true) };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ConnectorNotConnected_IsSwallowedAndReturnsEmpty()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => throw new InvalidOperationException("should never be called")));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, false) };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_SecondRunOverSameRange_InsertsNothingNewAndPreservesEditedStatus()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1", At(9, 0), At(9, 10)),
            Signal(ConnectorKey.Jira, "J-2:worklog:1", "J-2", At(12, 0), At(12, 10)),
        ])));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer(ConnectorKey.Jira, true) };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var first = await service.GenerateAsync("user-1", Request(), CancellationToken.None);
        first.IsError.Should().BeFalse();
        first.Value.Should().HaveCount(2);
        repository.Items.Should().HaveCount(2);

        repository.Items[0].Status = SuggestionStatus.Edited;

        var second = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        second.IsError.Should().BeFalse();
        second.Value.Should().BeEmpty();
        repository.Items.Should().HaveCount(2);
        repository.Items[0].Status.Should().Be(SuggestionStatus.Edited);
        repository.Items[1].Status.Should().Be(SuggestionStatus.Pending);
    }

    [Fact]
    public async Task ListAsync_NeverReturnsAnotherUsersSuggestions()
    {
        var repository = new FakeSuggestionRepository();
        await repository.Save(BuildModel("user-a", "MINE", At(9, 0), At(9, 30)));
        await repository.Save(BuildModel("user-b", "OTHER", At(9, 0), At(9, 30)));

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.ListAsync("user-a", From, To);

        result.Should().ContainSingle();
        result[0].TaskId.Should().Be("MINE");
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "MINE", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-b", model.Id, UpdateContract("CHANGED", At(9, 0), At(10, 0)), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSuggestion_UpdatesFieldsAndSetsEditedStatus()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "ORIGINAL", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-a", model.Id, UpdateContract("CHANGED", At(10, 0), At(11, 0)), CancellationToken.None);

        result.IsError.Should().BeFalse();
        var updated = result.Value;
        updated.TaskId.Should().Be("CHANGED");
        updated.DateStarted.Should().Be(At(10, 0));
        updated.DateEnded.Should().Be(At(11, 0));
        updated.Status.Should().Be(nameof(SuggestionStatus.Edited));
    }

    [Fact]
    public async Task UpdateAsync_MissingSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-a", "missing-id", UpdateContract("CHANGED", At(9, 0), At(10, 0)), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DismissAsync_ExistingSuggestion_SetsDismissedStatus()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "ORIGINAL", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-a", model.Id, CancellationToken.None);

        result.IsError.Should().BeFalse();
        repository.Items.Should().ContainSingle(x => x.Status == SuggestionStatus.Dismissed);
    }

    [Fact]
    public async Task AcceptAsync_ExistingSuggestion_ConfirmsAndExcludesFromList()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "ORIGINAL", At(9, 0), At(9, 30));
        await repository.Save(model);
        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.AcceptAsync("user-a", model.Id, CancellationToken.None);
        var listed = await service.ListAsync("user-a", From, To);

        result.IsError.Should().BeFalse();
        repository.Items.Should().ContainSingle(x => x.Status == SuggestionStatus.Confirmed);
        listed.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ManualEntryWithSameTaskId_SkipsSuggestion()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1", At(9, 0), At(9, 10)),
        ])));
        var timeEntries = new StubTimeEntryService
        {
            ListResult = ((IReadOnlyList<TimeEntryContract>)new List<TimeEntryContract>
            {
                new() { UserId = "user-1", TaskId = "J-1", Project = new ProjectContract("p", "P"), Activity = new ActivityContract("a", "A"), DateStarted = At(8, 30), DateEnded = At(9, 5) },
            }).ToErrorOr(),
        };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)], repository, timeEntries);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ManualEntryOnDifferentDayInRange_DoesNotSkipSuggestion()
    {
        var thursdayStart = BaseDate.AddDays(3) + TimeSpan.FromHours(9);
        var thursdayEnd = BaseDate.AddDays(3) + TimeSpan.FromHours(9.5);

        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:2", "J-1", thursdayStart, thursdayEnd),
        ])));
        var timeEntries = new StubTimeEntryService
        {
            ListResult = ((IReadOnlyList<TimeEntryContract>)new List<TimeEntryContract>
            {
                new() { UserId = "user-1", TaskId = "J-1", Project = new ProjectContract("p", "P"), Activity = new ActivityContract("a", "A"), DateStarted = At(8, 0), DateEnded = At(8, 30) },
            }).ToErrorOr(),
        };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)], repository, timeEntries);

        var request = new GenerateSuggestionsRequestContract { From = BaseDate, To = BaseDate.AddDays(4) };
        var result = await service.GenerateAsync("user-1", request, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        repository.Items.Should().ContainSingle(x => x.TaskId == "J-1" && x.DateStarted == thursdayStart);
    }

    [Fact]
    public async Task GenerateAsync_ManualEntrySameDayNonOverlappingTime_DoesNotSkipSuggestion()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:2", "J-1", At(17, 0), At(18, 0)),
        ])));
        var timeEntries = new StubTimeEntryService
        {
            ListResult = ((IReadOnlyList<TimeEntryContract>)new List<TimeEntryContract>
            {
                new() { UserId = "user-1", TaskId = "J-1", Project = new ProjectContract("p", "P"), Activity = new ActivityContract("a", "A"), DateStarted = At(13, 0), DateEnded = At(14, 0) },
            }).ToErrorOr(),
        };
        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)], repository, timeEntries);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        repository.Items.Should().ContainSingle(x => x.TaskId == "J-1" && x.DateStarted == At(17, 0));
    }

    [Fact]
    public async Task ReloadAsync_Success_ResetsMutableSuggestionsAndKeepsConfirmed()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Signals(
        [
            Signal(ConnectorKey.Jira, "J-1:worklog:1", "J-1", At(9, 0), At(9, 10)),
            Signal(ConnectorKey.Jira, "J-2:worklog:1", "J-2", At(10, 0), At(10, 10)),
        ])));
        var repository = new FakeSuggestionRepository();
        var edited = BuildModel("user-1", "J-1", At(9, 0), At(9, 30), "J-1:worklog:1");
        edited.Status = SuggestionStatus.Edited;
        var confirmed = BuildModel("user-1", "J-2", At(10, 0), At(10, 30), "J-2:worklog:1");
        confirmed.Status = SuggestionStatus.Confirmed;
        await repository.Save(edited);
        await repository.Save(confirmed);
        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)], repository);

        var result = await service.ReloadAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().ContainSingle();
        repository.Items.Should().HaveCount(2);
        repository.Items.Should().ContainSingle(x => x.Status == SuggestionStatus.Confirmed && x.TaskId == "J-2");
        repository.Items.Should().ContainSingle(x => x.Status == SuggestionStatus.Pending && x.TaskId == "J-1");
    }

    [Fact]
    public async Task ReloadAsync_FetchFails_DoesNotDeleteExistingSuggestions()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor(ConnectorKey.Jira), (_, _) => Fail(Error.Failure())));
        var repository = new FakeSuggestionRepository();
        await repository.Save(BuildModel("user-1", "KEEP", At(9, 0), At(9, 30), "J-1"));
        var service = CreateService(registry, [new FakeConnectorContextInitializer(ConnectorKey.Jira, true)], repository);

        var result = await service.ReloadAsync("user-1", Request(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        repository.Items.Should().ContainSingle(x => x.TaskId == "KEEP");
    }

    private static SuggestionService CreateService(FakeConnectorRegistry registry, IEnumerable<IConnectorContextInitializer> initializers, FakeSuggestionRepository repository, ITimeEntryService? timeEntryService = null)
    {
        var engine = new SuggestionEngine(new EchoSuggestionProvider());
        var resolver = new ConnectorResolver(registry, initializers);
        timeEntryService ??= new StubTimeEntryService { ListResult = ((IReadOnlyList<TimeEntryContract>)Array.Empty<TimeEntryContract>()).ToErrorOr() };
        return new SuggestionService(registry, resolver, repository, engine, timeEntryService, NullLogger<SuggestionService>.Instance);
    }

    private static Task<ErrorOr<IReadOnlyList<ActivitySignal>>> Signals(IReadOnlyList<ActivitySignal> signals) =>
        Task.FromResult<ErrorOr<IReadOnlyList<ActivitySignal>>>(signals.ToErrorOr());

    private static Task<ErrorOr<IReadOnlyList<ActivitySignal>>> Fail(Error error) =>
        Task.FromResult<ErrorOr<IReadOnlyList<ActivitySignal>>>(error);

    private static GenerateSuggestionsRequestContract Request() => new() { From = From, To = To };

    private static SuggestionUpdateContract UpdateContract(string taskId, DateTime start, DateTime end) => new()
    {
        TaskId = taskId,
        DateStarted = start,
        DateEnded = end,
    };

    private static SuggestionModel BuildModel(string userId, string taskId, DateTime start, DateTime end, string? externalId = null) => new()
    {
        UserId = userId,
        TaskId = taskId,
        DateStarted = start,
        DateEnded = end,
        Status = SuggestionStatus.Pending,
        Sources = externalId is null ? [] : [new SuggestionEvidenceModel { ConnectorKey = ConnectorKey.Jira, ExternalId = externalId }],
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

    private static ActivitySignal Signal(ConnectorKey connectorKey, string externalId, string subject, DateTime start, DateTime? end = null)
    {
        return new ActivitySignal
        {
            ConnectorKey = connectorKey,
            ExternalId = externalId,
            DateStarted = start,
            DateEnded = end,
            Kind = end.HasValue ? ActivityKind.Worklog : ActivityKind.Comment,
            Detail = new FakeSignalDetail(subject, null, null),
        };
    }
}
