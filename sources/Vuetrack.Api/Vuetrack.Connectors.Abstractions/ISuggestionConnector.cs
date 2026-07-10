namespace Vuetrack.Connectors.Abstractions;

public interface ISuggestionConnector
{
    ConnectorDescriptor Descriptor { get; }

    Task<ValidationOutcome> ValidateAsync(CancellationToken cancellationToken);

    Task<FetchOutcome> FetchAsync(FetchContainer request, CancellationToken cancellationToken);
}
