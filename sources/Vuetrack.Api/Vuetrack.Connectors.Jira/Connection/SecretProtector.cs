using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Jira.Connection;

[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class SecretProtector(IDataProtectionProvider provider)
    : SecretProtectorBase(provider, "Vuetrack.Connectors.Jira.RefreshToken.v1"), ISecretProtector
{
}

public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string ciphertext);
}
