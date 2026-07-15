using System.Text.Json;
using AwesomeAssertions;
using Vuetrack.Api.Features.Suggestions.Core;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Suggestions.Core;

public class SuggestionMetadataJsonTests
{
    [Fact]
    public void SerializeThenDeserialize_PreservesTypedValues()
    {
        var original = new Dictionary<string, JsonElement>
        {
            ["display.title"] = Element("\"PROJ-1 Fix\""),
            ["weight"] = Element("0.6"),
            ["jira.transition"] = Element("""{ "fromId": "1", "toId": "3" }"""),
        };

        var json = SuggestionMetadataJson.Serialize(original);
        var roundTripped = SuggestionMetadataJson.Deserialize(json);

        roundTripped.Should().ContainKey("display.title");
        roundTripped["display.title"].GetString().Should().Be("PROJ-1 Fix");
        roundTripped["weight"].GetDouble().Should().Be(0.6);
        roundTripped["jira.transition"].GetProperty("toId").GetString().Should().Be("3");
    }

    [Fact]
    public void Deserialize_NullOrEmpty_ReturnsEmptyDictionary()
    {
        SuggestionMetadataJson.Deserialize(null).Should().BeEmpty();
        SuggestionMetadataJson.Deserialize(string.Empty).Should().BeEmpty();
    }

    private static JsonElement Element(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
