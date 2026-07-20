using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Abstractions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TextDetailField), "text")]
[JsonDerivedType(typeof(DateDetailField), "date")]
[JsonDerivedType(typeof(LinkDetailField), "link")]
[JsonDerivedType(typeof(ChipDetailField), "chip")]
public abstract record DetailField
{
    public required DetailFieldLabel Label { get; init; }
}

public sealed record TextDetailField : DetailField
{
    public required string Value { get; init; }
}

public sealed record DateDetailField : DetailField
{
    public required DateTime Value { get; init; }
}

public sealed record LinkDetailField : DetailField
{
    public required string Text { get; init; }

    public required string Url { get; init; }
}

public sealed record ChipDetailField : DetailField
{
    public required string Value { get; init; }
}

public enum DetailFieldLabel
{
    Title,
    IssueType,
    Status,
    IssueKey,
    IssueLink,
}
