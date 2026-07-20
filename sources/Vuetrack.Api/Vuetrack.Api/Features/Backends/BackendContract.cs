using Vuetrack.Backends.Abstractions;

namespace Vuetrack.Api.Features.Backends;

public sealed record BackendContract(BackendKey Key, IReadOnlyList<BackendCapabilities> Capabilities, bool Connected, bool Healthy);
