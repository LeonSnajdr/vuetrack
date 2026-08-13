using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Tests.Fakes;

public sealed record FakeSignalDetail(string? TaskId, string? Project, string? Comment) : IActivitySignalDetail;
