using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed record FakeSignalDetail(string? TaskId, string? Project, string? Comment) : IConnectorSignalDetail;
