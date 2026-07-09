using Vuetrack.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Jira.Mapping;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraActivityMapperTests
{
    private const string SiteUrl = "https://acme.atlassian.net";

    private static readonly JiraMapperContext Context = new("jira", SiteUrl);

    private static JiraWorklogResponse Worklog(string issueKey = "PROJ-1", string worklogId = "100") => new()
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

    private static JiraIssueActivityResponse Issue(string issueKey = "PROJ-2") => new()
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

        Assert.Equal("jira", signal.ConnectorKey);
        Assert.Equal("PROJ-1:worklog:100", signal.ExternalId);
        Assert.Equal("PROJ-1 Fix login", signal.Title);
        Assert.Equal("worked on it", signal.Description);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero), signal.Start);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero), signal.End);
        Assert.Equal("https://acme.atlassian.net/browse/PROJ-1", signal.Link);
        Assert.Equal("PROJ-1", signal.Metadata["issueKey"]);
        Assert.Equal("100", signal.Metadata["worklogId"]);
        Assert.Equal("PROJ", signal.Metadata["project"]);
        Assert.Equal("Bug", signal.Metadata["issueType"]);
        Assert.Equal("In Progress", signal.Metadata["status"]);
    }

    [Fact]
    public void ToActivitySignal_Issue_ProducesDerivedSignalWithNoEnd()
    {
        var signal = Issue().ToActivitySignal(Context);

        Assert.Equal("PROJ-2:issue", signal.ExternalId);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 15, 0, 0, TimeSpan.Zero), signal.Start);
        Assert.Null(signal.End);
        Assert.Null(signal.Description);
    }
}
