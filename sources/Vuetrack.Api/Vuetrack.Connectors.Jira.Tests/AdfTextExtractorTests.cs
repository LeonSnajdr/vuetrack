using System.Text.Json;
using AwesomeAssertions;
using Vuetrack.Connectors.Jira.Internal;
using Xunit;

namespace Vuetrack.Connectors.Jira.Tests;

public class AdfTextExtractorTests
{
    [Fact]
    public void Extract_FlattensParagraphsAndText()
    {
        var adf = Parse("""
        {
          "type": "doc",
          "version": 1,
          "content": [
            { "type": "paragraph", "content": [ { "type": "text", "text": "Fixed the login bug" } ] },
            { "type": "paragraph", "content": [ { "type": "text", "text": "and added a test" } ] }
          ]
        }
        """);

        var result = AdfTextExtractor.Extract(adf);

        result.Should().Be("Fixed the login bug\nand added a test");
    }

    [Fact]
    public void Extract_HandlesHardBreaks()
    {
        var adf = Parse("""
        {
          "type": "doc",
          "content": [
            { "type": "paragraph", "content": [
              { "type": "text", "text": "line one" },
              { "type": "hardBreak" },
              { "type": "text", "text": "line two" }
            ] }
          ]
        }
        """);

        AdfTextExtractor.Extract(adf).Should().Be("line one\nline two");
    }

    [Fact]
    public void Extract_ReturnsNullForNullInput()
    {
        AdfTextExtractor.Extract(null).Should().BeNull();
    }

    [Fact]
    public void Extract_ReturnsNullForEmptyDocument()
    {
        var adf = Parse("""{ "type": "doc", "content": [] }""");
        AdfTextExtractor.Extract(adf).Should().BeNull();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
