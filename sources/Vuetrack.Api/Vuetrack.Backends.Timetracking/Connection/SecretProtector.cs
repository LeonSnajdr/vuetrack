using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class SecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private const string Purpose = "Vuetrack.Backends.Timetracking.RefreshToken.v1";

    private IDataProtector Protector { get; } = provider.CreateProtector(Purpose);

    public string Protect(string plaintext) => Protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => Protector.Unprotect(ciphertext);
}

public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string ciphertext);
}
