using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
    private static readonly DateTimeOffset BaseDate = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset From = BaseDate;

    private static readonly DateTimeOffset To = BaseDate.AddDays(1);

    [Fact]
    public async Task GenerateAsync_MultipleConnectors_AggregatesSignalsAndPersistsAllSuggestions()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor("jira"), (_, _) => Task.FromResult<ActivityFetchResult>(
            new ActivityFetchSuccess([Signal("jira", "J-1:worklog:1", "J-1 work", At(9, 0), At(9, 10))]))));
        registry.Add(new FakeConnector(Descriptor("other"), (_, _) => Task.FromResult<ActivityFetchResult>(
            new ActivityFetchSuccess([Signal("other", "O-1:worklog:1", "O-1 work", At(11, 0), At(11, 10))]))));

        var initializers = new IConnectorContextInitializer[]
        {
            new FakeConnectorContextInitializer("jira", true),
            new FakeConnectorContextInitializer("other", true),
        };

        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.GeneratedCount.Should().Be(2);
        result.ConnectorOutcomes.Should().HaveCount(2);
        result.ConnectorOutcomes.Should().OnlyContain(o => o.Status == "Success" && o.SignalCount == 1);
        repository.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateAsync_MixedConnectorOutcomes_NeverThrowsAndRecordsEachOutcome()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor("throws"), (_, _) => throw new InvalidOperationException("boom")));
        registry.Add(new FakeConnector(Descriptor("authfail"), (_, _) => Task.FromResult<ActivityFetchResult>(new ActivityFetchAuthFailed("nope"))));
        registry.Add(new FakeConnector(Descriptor("ratelimited"), (_, _) => Task.FromResult<ActivityFetchResult>(new ActivityFetchRateLimited(TimeSpan.FromSeconds(30)))));
        registry.Add(new FakeConnector(Descriptor("notconnected"), (_, _) => throw new InvalidOperationException("should never be called")));
        registry.Add(new FakeConnector(Descriptor("success"), (_, _) => Task.FromResult<ActivityFetchResult>(
            new ActivityFetchSuccess([Signal("success", "S-1:worklog:1", "S-1 work", At(9, 0), At(9, 10))]))));

        var initializers = new IConnectorContextInitializer[]
        {
            new FakeConnectorContextInitializer("throws", true),
            new FakeConnectorContextInitializer("authfail", true),
            new FakeConnectorContextInitializer("ratelimited", true),
            new FakeConnectorContextInitializer("notconnected", false),
            new FakeConnectorContextInitializer("success", true),
        };

        var repository = new FakeSuggestionRepository();
        var service = CreateService(registry, initializers, repository);

        var result = await service.GenerateAsync("user-1", Request(), CancellationToken.None);

        result.ConnectorOutcomes.Should().HaveCount(5);
        result.ConnectorOutcomes.First(o => o.ConnectorKey == "throws").Status.Should().Be("Error");
        result.ConnectorOutcomes.First(o => o.ConnectorKey == "authfail").Status.Should().Be("AuthFailed");
        result.ConnectorOutcomes.First(o => o.ConnectorKey == "ratelimited").Status.Should().Be("RateLimited");
        result.ConnectorOutcomes.First(o => o.ConnectorKey == "notconnected").Status.Should().Be("NotConnected");
        result.ConnectorOutcomes.First(o => o.ConnectorKey == "success").Status.Should().Be("Success");
        result.GeneratedCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateAsync_SecondRunOverSameRange_InsertsNothingNewAndPreservesEditedStatus()
    {
        var registry = new FakeConnectorRegistry();
        registry.Add(new FakeConnector(Descriptor("jira"), (_, _) => Task.FromResult<ActivityFetchResult>(new ActivityFetchSuccess(
        [
            Signal("jira", "J-1:worklog:1", "J-1 work", At(9, 0), At(9, 10)),
            Signal("jira", "J-2:worklog:1", "J-2 work", At(12, 0), At(12, 10)),
        ]))));

        var initializers = new IConnectorContextInitializer[] { new FakeConnectorContextInitializer("jira", true) };
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

        result.Should().BeOfType<SuggestionNotFound>();
    }

    [Fact]
    public async Task DismissAsync_AnotherUsersSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Mine", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-b", model.Id, CancellationToken.None);

        result.Should().BeOfType<SuggestionDismissNotFound>();
    }

    [Fact]
    public async Task UpdateAsync_ExistingSuggestion_UpdatesFieldsAndSetsEditedStatus()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Original", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-a", model.Id, UpdateContract("Changed", At(10, 0), At(11, 0)), CancellationToken.None);

        var updated = result.Should().BeOfType<SuggestionUpdated>().Subject;
        updated.Suggestion.Title.Should().Be("Changed");
        updated.Suggestion.Start.Should().Be(At(10, 0));
        updated.Suggestion.End.Should().Be(At(11, 0));
        updated.Suggestion.Status.Should().Be(nameof(SuggestionStatus.Edited));
    }

    [Fact]
    public async Task UpdateAsync_MissingSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.UpdateAsync("user-a", "missing-id", UpdateContract("Changed", At(9, 0), At(10, 0)), CancellationToken.None);

        result.Should().BeOfType<SuggestionNotFound>();
    }

    [Fact]
    public async Task DismissAsync_ExistingSuggestion_SetsDismissedStatus()
    {
        var repository = new FakeSuggestionRepository();
        var model = BuildModel("user-a", "Original", At(9, 0), At(9, 30));
        await repository.Save(model);

        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-a", model.Id, CancellationToken.None);

        result.Should().BeOfType<SuggestionDismissed>();
        repository.Items.Should().ContainSingle(x => x.Status == SuggestionStatus.Dismissed);
    }

    [Fact]
    public async Task DismissAsync_MissingSuggestion_ReturnsNotFound()
    {
        var repository = new FakeSuggestionRepository();
        var service = CreateService(new FakeConnectorRegistry(), [], repository);

        var result = await service.DismissAsync("user-a", "missing-id", CancellationToken.None);

        result.Should().BeOfType<SuggestionDismissNotFound>();
    }

    private static SuggestionService CreateService(FakeConnectorRegistry registry, IEnumerable<IConnectorContextInitializer> initializers, FakeSuggestionRepository repository)
    {
        var engine = new SuggestionEngine(Options.Create(new SuggestionEngineOptions()));
        return new SuggestionService(registry, initializers, repository, engine, NullLogger<SuggestionService>.Instance);
    }

    private static GenerateSuggestionsRequestContract Request() => new() { From = From, To = To };

    private static SuggestionUpdateContract UpdateContract(string title, DateTimeOffset start, DateTimeOffset end) => new()
    {
        Title = title,
        Start = start,
        End = end,
    };

    private static SuggestionModel BuildModel(string userId, string title, DateTimeOffset start, DateTimeOffset end) => new()
    {
        UserId = userId,
        Title = title,
        Start = start,
        End = end,
        Status = SuggestionStatus.Pending,
        Sources = [],
        Confidence = 0.5,
        CreatedAt = start,
        UpdatedAt = start,
    };

    private static ConnectorDescriptor Descriptor(string key) => new()
    {
        Key = key,
        DisplayName = key,
        Capabilities = ConnectorCapabilities.None,
    };

    private static DateTimeOffset At(int hour, int minute) => BaseDate + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);

    private static ActivitySignal Signal(string connectorKey, string externalId, string title, DateTimeOffset start, DateTimeOffset? end = null) => new()
    {
        ConnectorKey = connectorKey,
        ExternalId = externalId,
        Title = title,
        Start = start,
        End = end,
    };
}
