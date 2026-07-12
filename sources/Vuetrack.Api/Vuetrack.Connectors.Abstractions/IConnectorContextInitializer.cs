namespace Vuetrack.Connectors.Abstractions;

public interface IConnectorContextInitializer
{
    ConnectorKey ConnectorKey { get; }

    Task<bool> TryInitializeAsync(string userId, CancellationToken cancellationToken);
}
