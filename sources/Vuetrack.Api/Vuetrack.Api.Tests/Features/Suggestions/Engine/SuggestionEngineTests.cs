using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Connectors.Abstractions;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions.Engine;

public class SuggestionEngineTests
{
    private static readonly DateTimeOffset BaseDate = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset From = BaseDate;

    private static readonly DateTimeOffset To = BaseDate.AddDays(1);

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyList()
    {
        var engine = CreateEngine();

        var result = engine.Build([], From, To);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_SingleWorklogSignal_ProducesSingleSuggestionWithHighConfidence()
    {
        var engine = CreateEngine();
        var signal = Signal("PROJ-1:worklog:1", "PROJ-1 Fix login", At(9, 3), At(9, 12));

        var result = engine.Build([signal], From, To);

        result.Should().HaveCount(1);
        var suggestion = result[0];
        suggestion.Title.Should().Be("PROJ-1 Fix login");
        suggestion.Start.Should().Be(At(9, 0));
        suggestion.End.Should().Be(At(9, 15));
        suggestion.Confidence.Should().BeApproximately(0.6, 0.0001);
        suggestion.Sources.Should().ContainSingle();
        suggestion.Sources[0].ConnectorKey.Should().Be("jira");
        suggestion.Sources[0].ExternalId.Should().Be("PROJ-1:worklog:1");
    }

    [Fact]
    public void Build_SameCorrelationWithinMergeGap_MergesIntoOneBlock()
    {
        var engine = CreateEngine();
        var first = Signal("PROJ-2:worklog:1", "PROJ-2 Work", At(10, 0), At(10, 5), correlationId: "PROJ-2");
        var second = Signal("PROJ-2:worklog:2", "PROJ-2 Work", At(10, 10), At(10, 15), correlationId: "PROJ-2");

        var result = engine.Build([first, second], From, To);

        result.Should().HaveCount(1);
        var suggestion = result[0];
        suggestion.Start.Should().Be(At(10, 0));
        suggestion.End.Should().Be(At(10, 15));
        suggestion.Confidence.Should().BeApproximately(0.75, 0.0001);
        suggestion.Sources.Should().HaveCount(2);
    }

    [Fact]
    public void Build_SameCorrelationSeparatedByLargeGap_ProducesTwoBlocks()
    {
        var engine = CreateEngine();
        var first = Signal("PROJ-3:worklog:1", "PROJ-3 Work", At(11, 0), At(11, 5), correlationId: "PROJ-3");
        var second = Signal("PROJ-3:worklog:2", "PROJ-3 Work", At(11, 25), At(11, 30), correlationId: "PROJ-3");

        var result = engine.Build([first, second], From, To);

        result.Should().HaveCount(2);
        result[0].Start.Should().Be(At(11, 0));
        result[0].End.Should().Be(At(11, 5));
        result[1].Start.Should().Be(At(11, 25));
        result[1].End.Should().Be(At(11, 30));
    }

    [Fact]
    public void Build_DifferentCorrelationsOverlappingInTime_NeverMerge()
    {
        var engine = CreateEngine();
        var first = Signal("PROJ-4:worklog:1", "PROJ-4 Work", At(12, 0), At(12, 30), correlationId: "PROJ-4");
        var second = Signal("PROJ-5:worklog:1", "PROJ-5 Work", At(12, 10), At(12, 40), correlationId: "PROJ-5");

        var result = engine.Build([first, second], From, To);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Build_DuplicateExternalId_IsDeduplicated()
    {
        var engine = CreateEngine();
        var first = Signal("PROJ-6:worklog:1", "PROJ-6 Work", At(13, 0), At(13, 10));
        var duplicate = Signal("PROJ-6:worklog:1", "PROJ-6 Work", At(13, 0), At(13, 10));

        var result = engine.Build([first, duplicate], From, To);

        result.Should().HaveCount(1);
        result[0].Sources.Should().ContainSingle();
    }

    [Fact]
    public void Build_SignalPartiallyOutsideRange_IsClippedToBoundary()
    {
        var engine = CreateEngine();
        var customFrom = At(9, 0);
        var customTo = At(10, 0);
        var signal = Signal("PROJ-7:worklog:1", "PROJ-7 Work", At(8, 50), At(9, 30));

        var result = engine.Build([signal], customFrom, customTo);

        result.Should().HaveCount(1);
        result[0].Start.Should().Be(At(9, 0));
        result[0].End.Should().Be(At(9, 30));
    }

    [Fact]
    public void Build_SignalFullyOutsideRange_IsDropped()
    {
        var engine = CreateEngine();
        var customFrom = At(9, 0);
        var customTo = At(10, 0);
        var signal = Signal("PROJ-8:worklog:1", "PROJ-8 Work", At(7, 0), At(7, 30));

        var result = engine.Build([signal], customFrom, customTo);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_PointEventWithNoEnd_UsesDefaultDurationAndLowerConfidence()
    {
        var engine = CreateEngine();
        var signal = Signal("PROJ-9:issue", "PROJ-9 Some issue", At(14, 0));

        var result = engine.Build([signal], From, To);

        result.Should().HaveCount(1);
        result[0].Start.Should().Be(At(14, 0));
        result[0].End.Should().Be(At(14, 15));
        result[0].Confidence.Should().BeApproximately(0.3, 0.0001);
    }

    [Fact]
    public void Build_AlreadyAlignedBoundaries_AreUnchanged()
    {
        var engine = CreateEngine();
        var signal = Signal("PROJ-10:worklog:1", "PROJ-10 Work", At(15, 0), At(15, 5));

        var result = engine.Build([signal], From, To);

        result[0].Start.Should().Be(At(15, 0));
        result[0].End.Should().Be(At(15, 5));
    }

    [Fact]
    public void Build_UnalignedBoundaries_RoundOutwardToRoundTo()
    {
        var engine = CreateEngine();
        var signal = Signal("PROJ-11:worklog:1", "PROJ-11 Work", At(16, 3), At(16, 7));

        var result = engine.Build([signal], From, To);

        result[0].Start.Should().Be(At(16, 0));
        result[0].End.Should().Be(At(16, 10));
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
        var signal = Signal("PROJ-12:worklog:1", "PROJ-12 Work", At(17, 0), At(17, 2));

        var result = engine.Build([signal], From, To);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_IsDeterministicAndOrdersByStartThenTitle()
    {
        var engine = CreateEngine();
        var charlie = Signal("PROJ-C:worklog:1", "Charlie", At(20, 0), At(20, 10), correlationId: "C");
        var alpha = Signal("PROJ-A:worklog:1", "Alpha", At(18, 0), At(18, 10), correlationId: "A");
        var bravo = Signal("PROJ-B:worklog:1", "Bravo", At(19, 0), At(19, 10), correlationId: "B");
        var signals = new[] { charlie, alpha, bravo };

        var first = engine.Build(signals, From, To);
        var second = engine.Build(signals, From, To);

        first.Should().BeEquivalentTo(second, options => options.WithStrictOrdering());
        first.Select(s => s.Title).Should().ContainInOrder("Alpha", "Bravo", "Charlie");
    }

    private static SuggestionEngine CreateEngine(SuggestionEngineOptions? options = null)
    {
        return new SuggestionEngine(Options.Create(options ?? new SuggestionEngineOptions()));
    }

    private static DateTimeOffset At(int hour, int minute) => BaseDate + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);

    private static ActivitySignal Signal(string externalId, string title, DateTimeOffset start, DateTimeOffset? end = null, string? correlationId = null, string connectorKey = "jira")
    {
        return new ActivitySignal
        {
            ConnectorKey = connectorKey,
            ExternalId = externalId,
            Title = title,
            Start = start,
            End = end,
            Metadata = correlationId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["correlationId"] = correlationId },
        };
    }
}
