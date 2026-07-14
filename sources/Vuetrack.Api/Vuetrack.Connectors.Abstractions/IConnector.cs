using ErrorOr;

namespace Vuetrack.Connectors.Abstractions;

public interface IConnector
{
    ConnectorDescriptor Descriptor { get; }

    Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken);
}
