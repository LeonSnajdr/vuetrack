using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Github.Connection;

// Marker interface so DI resolves this connector's protector (its own DataProtection purpose)
// unambiguously, even though other OAuthSecretProtectorBase singletons exist for Jira and Timetracking.
[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class GithubConnectorSecretProtector(IDataProtectionProvider provider) : OAuthSecretProtectorBase(provider, "Vuetrack.Connectors.Github.RefreshToken.v1"), IGithubConnectorSecretProtector;

public interface IGithubConnectorSecretProtector : IOAuthSecretProtectorBase;
