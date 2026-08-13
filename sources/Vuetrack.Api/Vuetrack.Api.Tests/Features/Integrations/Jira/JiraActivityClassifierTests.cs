using AwesomeAssertions;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Internal;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Integrations.Jira;

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
