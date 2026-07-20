using AwesomeAssertions;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity;
using Vuetrack.Connectors.Jira.Activity.Api;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraActivityMapperTests
{
    private static readonly JiraIssueContext Context = new()
    {
        Key = "PROJ-1",
        Id = "1001",
        IssueType = "Bug",
        Status = "In Progress",
    };

    [Fact]
    public void ToWorklogSignal_ProducesTimedSignalWithTypedMetadata()
    {
        var worklog = new JiraWorklogResponse
        {
            Id = "100",
            Author = new JiraUserResponse { AccountId = "acc-1" },
            Started = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            TimeSpentSeconds = 3600,
        };

        var signal = JiraActivityMapper.ToWorklogSignal(Context, worklog);

        signal.ConnectorKey.Should().Be(ConnectorKey.Jira);
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().Be(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));

        signal.Kind.Should().Be(ActivityKind.Worklog);
        var detail = Detail(signal);
        detail.IssueKey.Should().Be("PROJ-1");
        detail.IssueType.Should().Be("Bug");
        detail.Status.Should().Be("In Progress");
    }

    [Fact]
    public void ToChangeSignal_ClassifiesStatusTransitionAndCapturesTypedTransition()
    {
        var changelog = new JiraChangelogResponse
        {
            Id = "5000",
            Author = new JiraUserResponse { AccountId = "acc-1" },
            Created = new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
            Items =
            [
                new JiraChangelogItemResponse { Field = "status", FieldId = "status", FromString = "To Do", ToDisplay = "In Progress" },
            ],
        };

        var signal = JiraActivityMapper.ToChangeSignal(Context, changelog, changelog.Items[0], 0);

        signal.ExternalId.Should().Be("PROJ-1:changelog:5000:0");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().BeNull();
        signal.Kind.Should().Be(ActivityKind.StatusTransition);

        Detail(signal).Transition.Should().Be(new JiraFieldTransition("To Do", "In Progress"));
    }

    [Fact]
    public void ToCommentSignal_ProducesPointEvent()
    {
        var comment = new JiraCommentResponse
        {
            Id = "9000",
            Author = new JiraUserResponse { AccountId = "acc-1" },
            Created = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        var signal = JiraActivityMapper.ToCommentSignal(Context, comment);

        signal.ExternalId.Should().Be("PROJ-1:comment:9000");
        signal.DateEnded.Should().BeNull();
        signal.Kind.Should().Be(ActivityKind.Comment);
    }

    private static JiraSignalDetail Detail(ActivitySignal signal)
    {
        return signal.Detail.Should().BeOfType<JiraSignalDetail>().Which;
    }
}
