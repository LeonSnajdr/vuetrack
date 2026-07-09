using System.Text;
using System.Text.Json;

namespace Vuetrack.Connectors.Jira.Internal;

public static class AdfTextExtractor
{
    public static string? Extract(JsonElement? adf)
    {
        if (adf is not { ValueKind: JsonValueKind.Object } root)
        {
            return null;
        }

        var builder = new StringBuilder();
        Walk(root, builder);

        var text = builder.ToString().Trim();
        return text.Length == 0 ? null : text;
    }

    private static void Walk(JsonElement node, StringBuilder builder)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var type = node.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
            ? typeProp.GetString()
            : null;

        if (type == "text" && node.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
        {
            builder.Append(textProp.GetString());
        }

        if (type is "hardBreak")
        {
            builder.Append('\n');
        }

        if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                Walk(child, builder);
            }
        }

        if (type is "paragraph" or "heading" or "listItem" or "blockquote")
        {
            builder.Append('\n');
        }
    }
}
