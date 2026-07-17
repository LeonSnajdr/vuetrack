using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.OAuth;

namespace Vuetrack.Backends.Timetracking.Connection;

// Marker interface so DI resolves this backend's protector (its own DataProtection purpose)
// unambiguously, even though a second OAuthSecretProtectorBase singleton exists for Jira.
[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class TimetrackingSecretProtector(IDataProtectionProvider provider) : OAuthSecretProtectorBase(provider, "Vuetrack.Backends.Timetracking.RefreshToken.v1"), ITimetrackingSecretProtector;

public interface ITimetrackingSecretProtector : IOAuthSecretProtectorBase;
