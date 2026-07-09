using System.Text.Json;

namespace Vuetrack.Connectors.Jira.Internal;

internal static class JsonElementExtensions
{
    public static JsonElement? GetPropertyPath(this JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
            {
                return null;
            }

            current = next;
        }

        return current;
    }
}
