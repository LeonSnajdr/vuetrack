using AwesomeAssertions;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Integrations.Jira;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Internal;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Integrations.Jira;

public class JiraActivityMapperTests
{
    private static readonly JiraIssueContext Context = new()
    {
        Key = "PROJ-1",
        Id = "1001",
        Title = "Fix login",
        IssueType = "Bug",
        Status = "In Progress",
        ParentKey = "PROJ-9",
        ParentTitle = "Login epic",
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

        var signal = worklog.ToWorklogSignal(Context);

        signal.Key.Should().Be(IntegrationKey.Jira);
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().Be(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));

        signal.Kind.Should().Be(ActivityKind.Worklog);
        var detail = Detail(signal);
        detail.IssueKey.Should().Be("PROJ-1");
        detail.Title.Should().Be("Fix login");
        detail.IssueType.Should().Be("Bug");
        detail.Status.Should().Be("In Progress");
        detail.ParentKey.Should().Be("PROJ-9");
        detail.ParentTitle.Should().Be("Login epic");
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

        var item = changelog.Items[0];
        var kind = item.Classify();
        var signal = changelog.ToChangeSignal(Context, item, kind, 0);

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

        var signal = comment.ToCommentSignal(Context);

        signal.ExternalId.Should().Be("PROJ-1:comment:9000");
        signal.DateEnded.Should().BeNull();
        signal.Kind.Should().Be(ActivityKind.Comment);
    }

    private static JiraSignalDetail Detail(ActivitySignal signal)
    {
        return signal.Detail.Should().BeOfType<JiraSignalDetail>().Which;
    }
}
