using AwesomeAssertions;
using Vuetrack.Connectors.Jira.Containers;
using Vuetrack.Connectors.Jira.Mapping;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraActivityMapperTests
{
    private const string SiteUrl = "https://acme.atlassian.net";

    private static readonly JiraMapperContainer Context = new("jira", SiteUrl);

    private static JiraWorklogContainer Worklog(string issueKey = "PROJ-1", string worklogId = "100") => new()
    {
        IssueKey = issueKey,
        IssueSummary = "Fix login",
        WorklogId = worklogId,
        Started = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
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
        Updated = new DateTimeOffset(2026, 7, 1, 15, 0, 0, TimeSpan.Zero),
        Project = "PROJ",
        IssueType = "Task",
        Status = "Done",
    };

    [Fact]
    public void ToActivitySignal_Worklog_ProducesTimedSignalWithMetadataAndLink()
    {
        var signal = Worklog().ToActivitySignal(Context);

        signal.ConnectorKey.Should().Be("jira");
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.Title.Should().Be("PROJ-1 Fix login");
        signal.Description.Should().Be("worked on it");
        signal.Start.Should().Be(new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
        signal.End.Should().Be(new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero));
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
        signal.Start.Should().Be(new DateTimeOffset(2026, 7, 1, 15, 0, 0, TimeSpan.Zero));
        signal.End.Should().BeNull();
        signal.Description.Should().BeNull();
    }
}
