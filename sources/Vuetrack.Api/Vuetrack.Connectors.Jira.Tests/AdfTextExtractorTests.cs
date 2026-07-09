using System.Text.Json;
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

        Assert.Equal("Fixed the login bug\nand added a test", result);
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

        Assert.Equal("line one\nline two", AdfTextExtractor.Extract(adf));
    }

    [Fact]
    public void Extract_ReturnsNullForNullInput()
    {
        Assert.Null(AdfTextExtractor.Extract(null));
    }

    [Fact]
    public void Extract_ReturnsNullForEmptyDocument()
    {
        var adf = Parse("""{ "type": "doc", "content": [] }""");
        Assert.Null(AdfTextExtractor.Extract(adf));
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
