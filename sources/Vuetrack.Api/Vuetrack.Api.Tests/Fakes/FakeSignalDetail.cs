using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

// Minimal connector-agnostic detail for tests: carries just the facts the fake model client echoes back.
public sealed record FakeSignalDetail(string? TaskId, string? Project, string? Comment) : IConnectorSignalDetail;
