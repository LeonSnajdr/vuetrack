using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions.Engine;

public class SuggestionEngineTests
{
    private static readonly DateTime BaseDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime From = BaseDate;

    private static readonly DateTime To = BaseDate.AddDays(1);

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyList()
    {
        var engine = CreateEngine();

        var result = engine.Build([], From, To);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_SingleWorklog_PromotesAloneWithTaskIdFromSubject()
    {
        var engine = CreateEngine();
        var signal = Worklog("PROJ-1:worklog:1", "PROJ-1", At(9, 3), At(9, 12));

        var result = engine.Build([signal], From, To);

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.TaskId.Should().Be("PROJ-1");
        suggestion.DateStarted.Should().Be(At(9, 0));
        suggestion.DateEnded.Should().Be(At(9, 15));
        suggestion.Confidence.Should().BeApproximately(1.0, 0.0001);
        suggestion.Sources.Should().ContainSingle();
        suggestion.Sources[0].ConnectorKey.Should().Be(ConnectorKey.Jira);
        suggestion.Sources[0].ExternalId.Should().Be("PROJ-1:worklog:1");
    }

    [Fact]
    public void Build_LoneCorroboratingEvent_IsBelowThresholdAndDropped()
    {
        var engine = CreateEngine();
        var comment = Point("PROJ-9:comment:1", "PROJ-9", At(14, 0), ActivityKind.Comment);

        var result = engine.Build([comment], From, To);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_LoneStatusTransition_PromotesAlone()
    {
        var engine = CreateEngine();
        var status = Point("PROJ-10:changelog:1:0", "PROJ-10", At(15, 0), ActivityKind.StatusTransition);

        var result = engine.Build([status], From, To);

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.TaskId.Should().Be("PROJ-10");
        suggestion.Confidence.Should().BeApproximately(0.3, 0.0001);
        suggestion.Sources.Should().ContainSingle();
    }

    [Fact]
    public void Build_TwoCorroboratingEventsSameSubject_ReachThresholdAndPromote()
    {
        var engine = CreateEngine();
        var comment = Point("PROJ-9:comment:1", "PROJ-9", At(14, 0), ActivityKind.Comment);
        var status = Point("PROJ-9:changelog:1:0", "PROJ-9", At(14, 5), ActivityKind.StatusTransition);

        var result = engine.Build([comment, status], From, To);

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.Confidence.Should().BeApproximately(0.6, 0.0001);
        suggestion.Sources.Should().HaveCount(2);
    }

    [Fact]
    public void Build_SameSubjectWithinMergeGap_MergesIntoOneBlock()
    {
        var engine = CreateEngine();
        var first = Worklog("PROJ-2:worklog:1", "PROJ-2", At(10, 0), At(10, 5));
        var second = Worklog("PROJ-2:worklog:2", "PROJ-2", At(10, 10), At(10, 15));

        var result = engine.Build([first, second], From, To);

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.DateStarted.Should().Be(At(10, 0));
        suggestion.DateEnded.Should().Be(At(10, 15));
        suggestion.Sources.Should().HaveCount(2);
    }

    [Fact]
    public void Build_SameSubjectSeparatedByLargeGap_ProducesTwoBlocks()
    {
        var engine = CreateEngine();
        var first = Worklog("PROJ-3:worklog:1", "PROJ-3", At(11, 0), At(11, 5));
        var second = Worklog("PROJ-3:worklog:2", "PROJ-3", At(11, 25), At(11, 30));

        var result = engine.Build([first, second], From, To);

        result.Should().HaveCount(2);
        result[0].DateStarted.Should().Be(At(11, 0));
        result[1].DateStarted.Should().Be(At(11, 25));
    }

    [Fact]
    public void Build_DifferentSubjectsOverlappingInTime_NeverMerge()
    {
        var engine = CreateEngine();
        var first = Worklog("PROJ-4:worklog:1", "PROJ-4", At(12, 0), At(12, 30));
        var second = Worklog("PROJ-5:worklog:1", "PROJ-5", At(12, 10), At(12, 40));

        var result = engine.Build([first, second], From, To);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Build_DuplicateExternalId_IsDeduplicated()
    {
        var engine = CreateEngine();
        var first = Worklog("PROJ-6:worklog:1", "PROJ-6", At(13, 0), At(13, 10));
        var duplicate = Worklog("PROJ-6:worklog:1", "PROJ-6", At(13, 0), At(13, 10));

        var result = engine.Build([first, duplicate], From, To);

        result.Should().ContainSingle().Which.Sources.Should().ContainSingle();
    }

    [Fact]
    public void Build_SignalPartiallyOutsideRange_IsClippedToBoundary()
    {
        var engine = CreateEngine();
        var customFrom = At(9, 0);
        var customTo = At(10, 0);
        var signal = Worklog("PROJ-7:worklog:1", "PROJ-7", At(8, 50), At(9, 30));

        var result = engine.Build([signal], customFrom, customTo);

        var suggestion = result.Should().ContainSingle().Which;
        suggestion.DateStarted.Should().Be(At(9, 0));
        suggestion.DateEnded.Should().Be(At(9, 30));
    }

    [Fact]
    public void Build_SignalFullyOutsideRange_IsDropped()
    {
        var engine = CreateEngine();
        var signal = Worklog("PROJ-8:worklog:1", "PROJ-8", At(7, 0), At(7, 30));

        var result = engine.Build([signal], At(9, 0), At(10, 0));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_UnalignedBoundaries_RoundOutwardToRoundTo()
    {
        var engine = CreateEngine();
        var signal = Worklog("PROJ-11:worklog:1", "PROJ-11", At(16, 3), At(16, 7));

        var result = engine.Build([signal], From, To);

        result[0].DateStarted.Should().Be(At(16, 0));
        result[0].DateEnded.Should().Be(At(16, 10));
    }

    [Fact]
    public void Build_BlockShorterThanMinimumAfterRounding_IsDropped()
    {
        var options = Options.Create(new SuggestionEngineOptions
        {
            RoundTo = TimeSpan.FromMinutes(1),
            MinimumBlock = TimeSpan.FromMinutes(5),
        });
        var engine = new SuggestionEngine(options);
        var signal = Worklog("PROJ-12:worklog:1", "PROJ-12", At(17, 0), At(17, 2));

        var result = engine.Build([signal], From, To);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_IsDeterministicAndOrdersByStart()
    {
        var engine = CreateEngine();
        var charlie = Worklog("PROJ-C:worklog:1", "PROJ-C", At(20, 0), At(20, 10));
        var alpha = Worklog("PROJ-A:worklog:1", "PROJ-A", At(18, 0), At(18, 10));
        var bravo = Worklog("PROJ-B:worklog:1", "PROJ-B", At(19, 0), At(19, 10));
        var signals = new[] { charlie, alpha, bravo };

        var first = engine.Build(signals, From, To);
        var second = engine.Build(signals, From, To);

        first.Should().BeEquivalentTo(second, options => options.WithStrictOrdering());
        first.Select(s => s.TaskId).Should().ContainInOrder("PROJ-A", "PROJ-B", "PROJ-C");
    }

    private static SuggestionEngine CreateEngine(SuggestionEngineOptions? options = null)
    {
        return new SuggestionEngine(Options.Create(options ?? new SuggestionEngineOptions()));
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
        var builder = new SignalMetadataBuilder();
        builder.Set(MetadataKeys.ActivityKind, kind);
        builder.Set(MetadataKeys.SubjectWorkItemId, subject);
        builder.Set(MetadataKeys.CorrelationKeys, new List<string> { subject });
        builder.Set(MetadataKeys.DisplayTitle, $"{subject} work");

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = start,
            DateEnded = end,
            Metadata = builder.Build(),
        };
    }
}
