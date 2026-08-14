using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Tests.Fakes;

public sealed record FakeSignalDetail(string? TaskId, string? Project, string? Comment) : IActivitySignalDetail;
