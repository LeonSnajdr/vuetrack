namespace Vuetrack.Api.Features.Backends.Timetracking.Contracts;

public sealed record TimetrackingAuthorizeContract(string AuthorizationUrl, string State);
