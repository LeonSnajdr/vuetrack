using Microsoft.AspNetCore.DataProtection;

namespace Vuetrack.OAuth;

/// <summary>
/// Shared refresh-token protection over <see cref="IDataProtectionProvider"/>.
/// Concrete platforms supply their own DataProtection purpose so ciphertext stays isolated per platform.
/// </summary>
public abstract class SecretProtectorBase(IDataProtectionProvider provider, string purpose)
{
    private IDataProtector Protector { get; } = provider.CreateProtector(purpose);

    public string Protect(string plaintext) => Protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => Protector.Unprotect(ciphertext);
}
