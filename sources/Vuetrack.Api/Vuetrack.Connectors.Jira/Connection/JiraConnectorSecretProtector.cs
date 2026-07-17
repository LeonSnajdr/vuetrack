using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Jira.Connection;

// Marker interface so DI resolves this connector's protector (its own DataProtection purpose)
// unambiguously, even though a second OAuthSecretProtectorBase singleton exists for Timetracking.
[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class JiraConnectorSecretProtector(IDataProtectionProvider provider) : OAuthSecretProtectorBase(provider, "Vuetrack.Connectors.Jira.RefreshToken.v1"), IJiraConnectorSecretProtector;

public interface IJiraConnectorSecretProtector : IOAuthSecretProtectorBase;
