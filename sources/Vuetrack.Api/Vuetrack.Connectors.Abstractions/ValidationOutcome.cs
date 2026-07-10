namespace Vuetrack.Connectors.Abstractions;

public abstract record ValidationOutcome;

public sealed record ValidationValid() : ValidationOutcome;

public sealed record ValidationInvalid(IReadOnlyList<string> Errors) : ValidationOutcome;
