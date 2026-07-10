namespace Vuetrack.Connectors.Abstractions;

public interface IConnector
{
    ConnectorDescriptor Descriptor { get; }

    Task<ConnectorValidationResult> ValidateAsync(CancellationToken cancellationToken);

    Task<ActivityFetchResult> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken);
}
