using Microsoft.AspNetCore.DataProtection;

namespace Vuetrack.OAuth;

public abstract class OAuthSecretProtectorBase(IDataProtectionProvider provider, string purpose) : IOAuthSecretProtectorBase
{
    private IDataProtector Protector { get; } = provider.CreateProtector(purpose);

    public string Protect(string plaintext) => Protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => Protector.Unprotect(ciphertext);
}

public interface IOAuthSecretProtectorBase
{
    string Protect(string plaintext);

    string Unprotect(string ciphertext);
}
