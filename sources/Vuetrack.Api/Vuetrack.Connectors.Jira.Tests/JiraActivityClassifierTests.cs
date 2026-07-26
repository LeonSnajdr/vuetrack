using AwesomeAssertions;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity;
using Vuetrack.Connectors.Jira.Activity.Api;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class JiraActivityClassifierTests
{
    [Theory]
    [InlineData("status", ActivityKind.StatusTransition)]
    [InlineData("summary", ActivityKind.ContentEdit)]
    [InlineData("description", ActivityKind.ContentEdit)]
    [InlineData("priority", ActivityKind.Planning)]
    [InlineData("assignee", ActivityKind.Planning)]
    [InlineData("project", ActivityKind.Admin)]
    [InlineData("key", ActivityKind.Admin)]
    [InlineData("unknown", ActivityKind.Planning)]
    public void Classify_MapsFieldToKind(string field, ActivityKind expected)
    {
        var item = new JiraChangelogItemResponse { Field = field, FieldId = field };

        var kind = item.Classify();

        kind.Should().Be(expected);
    }

    [Fact]
    public void Classify_PrefersFieldIdOverField()
    {
        var item = new JiraChangelogItemResponse { Field = "summary", FieldId = "status" };

        var kind = item.Classify();

        kind.Should().Be(ActivityKind.StatusTransition);
    }
}
