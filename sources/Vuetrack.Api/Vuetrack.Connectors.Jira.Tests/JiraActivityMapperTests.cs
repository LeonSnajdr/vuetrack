using AwesomeAssertions;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraActivityMapperTests
{
    private const string SiteUrl = "https://acme.atlassian.net";

    private static readonly JiraMapperContext Context = new(ConnectorKey.Jira, SiteUrl);

    private static JiraWorklogContainer Worklog(string issueKey = "PROJ-1", string worklogId = "100") => new()
    {
        IssueKey = issueKey,
        IssueSummary = "Fix login",
        WorklogId = worklogId,
        Started = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
        TimeSpentSeconds = 3600,
        Comment = "worked on it",
        Project = "PROJ",
        IssueType = "Bug",
        Status = "In Progress",
    };

    private static JiraIssueActivityContainer Issue(string issueKey = "PROJ-2") => new()
    {
        IssueKey = issueKey,
        Summary = "Some other issue",
        Updated = new DateTime(2026, 7, 1, 15, 0, 0, DateTimeKind.Utc),
        Project = "PROJ",
        IssueType = "Task",
        Status = "Done",
    };

    [Fact]
    public void ToActivitySignal_Worklog_ProducesTimedSignalWithMetadataAndLink()
    {
        var signal = Worklog().ToActivitySignal(Context);

        signal.ConnectorKey.Should().Be(ConnectorKey.Jira);
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.Title.Should().Be("PROJ-1 Fix login");
        signal.Description.Should().Be("worked on it");
        signal.Metadata[ActivityMetadataKeys.TaskId].Should().Be("PROJ-1");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().Be(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        signal.Link.Should().Be("https://acme.atlassian.net/browse/PROJ-1");
        signal.Metadata["issueKey"].Should().Be("PROJ-1");
        signal.Metadata["worklogId"].Should().Be("100");
        signal.Metadata["project"].Should().Be("PROJ");
        signal.Metadata["issueType"].Should().Be("Bug");
        signal.Metadata["status"].Should().Be("In Progress");
    }

    [Fact]
    public void ToActivitySignal_Issue_ProducesDerivedSignalWithNoEnd()
    {
        var signal = Issue().ToActivitySignal(Context);

        signal.ExternalId.Should().Be("PROJ-2:issue");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 15, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().BeNull();
        signal.Description.Should().BeNull();
        signal.Metadata[ActivityMetadataKeys.TaskId].Should().Be("PROJ-2");
    }
}
