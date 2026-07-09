namespace Vuetrack.Connectors.Abstractions;

public abstract record ValidationOutcome;

public sealed record Valid() : ValidationOutcome;

public sealed record Invalid(IReadOnlyList<string> Errors) : ValidationOutcome;
