namespace Vuetrack.Connectors.Abstractions;

// Marker for the connector-specific detail carried by an ActivitySignal. The engine treats it as opaque;
// the model reasons over its serialized shape, and connector-aware code downcasts to the concrete record.
public interface IConnectorSignalDetail;
