using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Api.Features.Suggestions.Engine.Provider;
using Vuetrack.Api.Tests.Fakes;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions.Engine;

public class SuggestionEngineTests
{
    private static readonly DateTime BaseDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime From = BaseDate;

    private static readonly DateTime To = BaseDate.AddDays(1);

    [Fact]
    public async Task BuildAsync_EmptyInput_ReturnsEmptyAndDoesNotCallProvider()
    {
        var provider = new FakeSuggestionProvider();
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([], From, To, CancellationToken.None)).Value;

        result.Should().BeEmpty();
        provider.LastContext.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_SendsEverySignalToProvider()
    {
        var provider = new FakeSuggestionProvider();
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);
        var worklog = Worklog("PROJ-1:worklog:1", "PROJ-1", At(9, 0), At(9, 30));
        var comment = Point("PROJ-2:comment:1", "PROJ-2", At(10, 0), ActivityKind.Comment);

        await engine.BuildAsync([worklog, comment], From, To, CancellationToken.None);

        provider.LastContext.Should().NotBeNull();
        provider.LastContext!.From.Should().Be(From);
        provider.LastContext.To.Should().Be(To);
        provider.LastContext.Signals.Select(s => s.ExternalId).Should().BeEquivalentTo("PROJ-1:worklog:1", "PROJ-2:comment:1");
        var worklogSignal = provider.LastContext.Signals.Single(s => s.ExternalId == "PROJ-1:worklog:1");
        worklogSignal.DateEnded.Should().Be(At(9, 30));
        worklogSignal.Kind.Should().Be(ActivityKind.Worklog);
        var detail = worklogSignal.Detail.Should().BeOfType<FakeSignalDetail>().Which;
        detail.TaskId.Should().Be("PROJ-1");
    }

    [Fact]
    public async Task BuildAsync_MapsCandidateAndRebuildsEvidenceFromSources()
    {
        var signal = Worklog("PROJ-1:worklog:1", "PROJ-1", At(9, 0), At(9, 30));
        var candidate = new SuggestionProviderCandidate
        {
            TaskId = "PROJ-1",
            Comment = "Worked on PROJ-1",
            DateStarted = At(9, 0),
            DateEnded = At(9, 30),
            Confidence = 0.9,
            SourceExternalIds = ["PROJ-1:worklog:1"],
        };
        var provider = new FakeSuggestionProvider(candidate);
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([signal], From, To, CancellationToken.None)).Value;

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.TaskId.Should().Be("PROJ-1");
        suggestion.Comment.Should().Be("Worked on PROJ-1");
        suggestion.DateStarted.Should().Be(At(9, 0));
        suggestion.DateEnded.Should().Be(At(9, 30));
        suggestion.Confidence.Should().BeApproximately(0.9, 0.0001);
        var evidence = suggestion.Sources.Should().ContainSingle().Which;
        evidence.Key.Should().Be(IntegrationKey.Jira);
        evidence.ExternalId.Should().Be("PROJ-1:worklog:1");
        evidence.Kind.Should().Be(ActivityKind.Worklog);
        evidence.DateStarted.Should().Be(At(9, 0));
        evidence.DateEnded.Should().Be(At(9, 30));
    }

    [Fact]
    public async Task BuildAsync_PointEventEvidence_UsesStartAsEndWhenNoDuration()
    {
        var signal = Point("PROJ-2:comment:1", "PROJ-2", At(11, 0), ActivityKind.Comment);
        var candidate = new SuggestionProviderCandidate
        {
            DateStarted = At(11, 0),
            DateEnded = At(11, 20),
            Confidence = 0.4,
            SourceExternalIds = ["PROJ-2:comment:1"],
        };
        var provider = new FakeSuggestionProvider(candidate);
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([signal], From, To, CancellationToken.None)).Value;

        var evidence = result.Should().ContainSingle().Which.Sources.Should().ContainSingle().Which;
        evidence.DateStarted.Should().Be(At(11, 0));
        evidence.DateEnded.Should().Be(At(11, 0));
    }

    [Fact]
    public async Task BuildAsync_UnknownSourceIds_AreDroppedAndSuggestionWithoutKnownSourcesIsDropped()
    {
        var known = Worklog("PROJ-1:worklog:1", "PROJ-1", At(9, 0), At(9, 30));
        var withUnknown = new SuggestionProviderCandidate
        {
            DateStarted = At(9, 0),
            DateEnded = At(9, 30),
            Confidence = 0.9,
            SourceExternalIds = ["PROJ-1:worklog:1", "does-not-exist"],
        };
        var allUnknown = new SuggestionProviderCandidate
        {
            DateStarted = At(13, 0),
            DateEnded = At(13, 30),
            Confidence = 0.9,
            SourceExternalIds = ["ghost:1"],
        };
        var provider = new FakeSuggestionProvider(withUnknown, allUnknown);
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([known], From, To, CancellationToken.None)).Value;

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.Sources.Should().ContainSingle().Which.ExternalId.Should().Be("PROJ-1:worklog:1");
    }

    [Fact]
    public async Task BuildAsync_ClampsToWindowAndDropsInverted()
    {
        var signal = Worklog("PROJ-1:worklog:1", "PROJ-1", At(9, 0), At(9, 30));
        var customFrom = At(9, 0);
        var customTo = At(10, 0);
        var spillsOver = new SuggestionProviderCandidate
        {
            DateStarted = At(8, 30),
            DateEnded = At(9, 45),
            Confidence = 1.0,
            SourceExternalIds = ["PROJ-1:worklog:1"],
        };
        var provider = new FakeSuggestionProvider(spillsOver);
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([signal], customFrom, customTo, CancellationToken.None)).Value;

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.DateStarted.Should().Be(At(9, 0));
        suggestion.DateEnded.Should().Be(At(9, 45));
    }

    [Fact]
    public async Task BuildAsync_ConfidenceIsClampedToOne()
    {
        var signal = Worklog("PROJ-1:worklog:1", "PROJ-1", At(9, 0), At(9, 30));
        var candidate = new SuggestionProviderCandidate
        {
            DateStarted = At(9, 0),
            DateEnded = At(9, 30),
            Confidence = 1.7,
            SourceExternalIds = ["PROJ-1:worklog:1"],
        };
        var provider = new FakeSuggestionProvider(candidate);
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([signal], From, To, CancellationToken.None)).Value;

        result.Should().ContainSingle().Which.Confidence.Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public async Task BuildAsync_DeduplicatesSignalsByConnectorAndExternalIdBeforeProvider()
    {
        var first = Worklog("PROJ-6:worklog:1", "PROJ-6", At(13, 0), At(13, 10));
        var duplicate = Worklog("PROJ-6:worklog:1", "PROJ-6", At(13, 0), At(13, 10));
        var provider = new FakeSuggestionProvider();
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        await engine.BuildAsync([first, duplicate], From, To, CancellationToken.None);

        provider.LastContext!.Signals.Should().ContainSingle();
    }

    [Fact]
    public async Task BuildAsync_OrdersResultsByStart()
    {
        var alpha = Worklog("PROJ-A:worklog:1", "PROJ-A", At(18, 0), At(18, 10));
        var bravo = Worklog("PROJ-B:worklog:1", "PROJ-B", At(19, 0), At(19, 10));
        var charlie = Worklog("PROJ-C:worklog:1", "PROJ-C", At(20, 0), At(20, 10));
        var candidates = new[]
        {
            Candidate("PROJ-C:worklog:1", At(20, 0), At(20, 10)),
            Candidate("PROJ-A:worklog:1", At(18, 0), At(18, 10)),
            Candidate("PROJ-B:worklog:1", At(19, 0), At(19, 10)),
        };
        var provider = new FakeSuggestionProvider(candidates);
        var engine = new SuggestionEngine(provider, NullLogger<SuggestionEngine>.Instance);

        var result = (await engine.BuildAsync([charlie, alpha, bravo], From, To, CancellationToken.None)).Value;

        result.Select(s => s.Sources[0].ExternalId).Should().ContainInOrder("PROJ-A:worklog:1", "PROJ-B:worklog:1", "PROJ-C:worklog:1");
    }

    private static SuggestionProviderCandidate Candidate(string externalId, DateTime start, DateTime end)
    {
        return new SuggestionProviderCandidate
        {
            DateStarted = start,
            DateEnded = end,
            Confidence = 1.0,
            SourceExternalIds = [externalId],
        };
    }

    private static DateTime At(int hour, int minute) => BaseDate + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);

    private static ActivitySignal Worklog(string externalId, string subject, DateTime start, DateTime end)
    {
        return Build(externalId, subject, start, end, ActivityKind.Worklog);
    }

    private static ActivitySignal Point(string externalId, string subject, DateTime start, ActivityKind kind)
    {
        return Build(externalId, subject, start, null, kind);
    }

    private static ActivitySignal Build(string externalId, string subject, DateTime start, DateTime? end, ActivityKind kind)
    {
        return new ActivitySignal
        {
            Key = IntegrationKey.Jira,
            ExternalId = externalId,
            DateStarted = start,
            DateEnded = end,
            Kind = kind,
            Detail = new FakeSignalDetail(subject, null, null),
        };
    }

    private sealed class FakeSuggestionProvider(params SuggestionProviderCandidate[] candidates) : ISuggestionProvider
    {
        private IReadOnlyList<SuggestionProviderCandidate> Candidates { get; } = candidates;

        public SuggestionProviderContext? LastContext { get; private set; }

        public Task<ErrorOr<IReadOnlyList<SuggestionProviderCandidate>>> ProvideAsync(SuggestionProviderContext context, CancellationToken cancellationToken)
        {
            LastContext = context;
            return Task.FromResult(Candidates.ToErrorOr());
        }
    }
}
