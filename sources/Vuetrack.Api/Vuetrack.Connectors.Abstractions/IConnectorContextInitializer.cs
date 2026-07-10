namespace Vuetrack.Connectors.Abstractions;

public interface IConnectorContextInitializer
{
    string ConnectorKey { get; }

    Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken);
}
