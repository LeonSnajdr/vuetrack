using System.Text.Json;
using AwesomeAssertions;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;
using Vuetrack.Connectors.Jira.Activity;
using Vuetrack.Connectors.Jira.Activity.Dtos;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraActivityMapperTests
{
    private const string SiteUrl = "https://acme.atlassian.net";

    private static readonly JiraIssueContext Context = new()
    {
        Key = "PROJ-1",
        Id = "1001",
        Summary = "Fix login",
        ProjectKey = "PROJ",
        ProjectName = "Project",
        IssueType = "Bug",
        Status = "In Progress",
    };

    [Fact]
    public void ToWorklogSignal_ProducesTimedSignalWithTypedMetadata()
    {
        var worklog = new JiraWorklogDto
        {
            Id = "100",
            Author = new JiraUserDto { AccountId = "acc-1" },
            Started = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
            TimeSpentSeconds = 3600,
            Comment = Adf("worked on it"),
        };

        var signal = JiraActivityMapper.ToWorklogSignal(Context, worklog, SiteUrl);

        signal.ConnectorKey.Should().Be(ConnectorKey.Jira);
        signal.ExternalId.Should().Be("PROJ-1:worklog:100");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().Be(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));

        Kind(signal).Should().Be(ActivityKind.Worklog);
        Get(signal, MetadataKeys.SubjectWorkItemId).Should().Be("PROJ-1");
        Get(signal, MetadataKeys.ActorId).Should().Be("acc-1");
        Get(signal, MetadataKeys.DisplayTitle).Should().Be("PROJ-1 Fix login");
        Get(signal, MetadataKeys.DisplayComment).Should().Be("worked on it");
        Get(signal, MetadataKeys.SourceUrl).Should().Be("https://acme.atlassian.net/browse/PROJ-1");
        Get(signal, MetadataKeys.DisplayProject).Should().Be("Project");
        Get(signal, JiraMetadataKeys.IssueType).Should().Be("Bug");
        Get(signal, JiraMetadataKeys.WorklogId).Should().Be("100");
    }

    [Fact]
    public void ToChangeSignal_ClassifiesStatusTransitionAndCapturesTypedTransition()
    {
        var changelog = new JiraChangelogDto
        {
            Id = "5000",
            Author = new JiraUserDto { AccountId = "acc-1" },
            Created = new DateTimeOffset(2026, 7, 1, 11, 0, 0, TimeSpan.Zero),
            Items =
            [
                new JiraChangelogItemDto { Field = "status", FieldId = "status", From = "1", FromString = "To Do", To = "3", ToDisplay = "In Progress" },
            ],
        };

        var signal = JiraActivityMapper.ToChangeSignal(Context, changelog, changelog.Items[0], 0, SiteUrl);

        signal.ExternalId.Should().Be("PROJ-1:changelog:5000:0");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc));
        signal.DateEnded.Should().BeNull();
        Kind(signal).Should().Be(ActivityKind.StatusTransition);

        signal.Metadata.TryGet(JiraMetadataKeys.Transition, out var transition).Should().BeTrue();
        transition.Should().Be(new JiraFieldTransition("1", "To Do", "3", "In Progress"));
    }

    [Fact]
    public void ToCommentSignal_ProducesPointEventWithCommentText()
    {
        var comment = new JiraCommentDto
        {
            Id = "9000",
            Author = new JiraUserDto { AccountId = "acc-1" },
            Created = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero),
            Body = Adf("looks good"),
        };

        var signal = JiraActivityMapper.ToCommentSignal(Context, comment, SiteUrl);

        signal.ExternalId.Should().Be("PROJ-1:comment:9000");
        signal.DateEnded.Should().BeNull();
        Kind(signal).Should().Be(ActivityKind.Comment);
        Get(signal, MetadataKeys.DisplayComment).Should().Be("looks good");
    }

    private static ActivityKind Kind(ActivitySignal signal)
    {
        signal.Metadata.TryGet(MetadataKeys.ActivityKind, out var kind).Should().BeTrue();
        return kind;
    }

    private static string? Get(ActivitySignal signal, MetadataKey<string> key)
    {
        signal.Metadata.TryGet(key, out var value);
        return value;
    }

    private static JsonElement Adf(string text)
    {
        var json = $$"""{ "type": "doc", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "{{text}}" } ] } ] }""";
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
