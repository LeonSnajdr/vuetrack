namespace Vuetrack.Connectors.Abstractions;

public abstract record ConnectorValidationResult;

public sealed record ConnectorValidationValid : ConnectorValidationResult;

public sealed record ConnectorValidationInvalid(IReadOnlyList<string> Errors) : ConnectorValidationResult;
