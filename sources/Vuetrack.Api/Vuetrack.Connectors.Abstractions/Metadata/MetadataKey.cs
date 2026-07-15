namespace Vuetrack.Connectors.Abstractions.Metadata;

// A strongly typed handle for a metadata value. The Name is the stable string key persisted
// in the signal metadata dictionary; T is the shape the value serializes to/from.
public sealed record MetadataKey<T>(string Name);
