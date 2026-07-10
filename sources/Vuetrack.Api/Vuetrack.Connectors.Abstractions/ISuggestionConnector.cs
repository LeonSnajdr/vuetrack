namespace Vuetrack.Connectors.Abstractions;

public interface ISuggestionConnector
{
    ConnectorDescriptor Descriptor { get; }

    Task<ValidationOutcome> ValidateAsync(CancellationToken cancellationToken);

    Task<FetchResult> FetchAsync(FetchContainer request, CancellationToken cancellationToken);
}
