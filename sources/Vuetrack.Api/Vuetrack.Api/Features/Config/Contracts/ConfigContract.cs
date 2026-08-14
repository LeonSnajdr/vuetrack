namespace Vuetrack.Api.Features.Config.Contracts;

public sealed record ConfigContract(AppAuthContract AuthOptions);

public sealed record AppAuthContract(string AuthUrl, string Realm, string AppClientId, string ApiClientId);
