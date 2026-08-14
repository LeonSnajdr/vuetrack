using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

[Inject(Target.Matching, ServiceLifetime.Singleton)]
public class ConnectionSecretProtector(IDataProtectionProvider provider) : IConnectionSecretProtector
{
    private IDataProtectionProvider Provider { get; } = provider;

    private ConcurrentDictionary<IntegrationKey, IDataProtector> Protectors { get; } = new();

    public string Protect(IntegrationKey key, string plaintext)
    {
        var protector = ResolveProtector(key);
        var ciphertext = protector.Protect(plaintext);

        return ciphertext;
    }

    public string Unprotect(IntegrationKey key, string ciphertext)
    {
        var protector = ResolveProtector(key);
        var plaintext = protector.Unprotect(ciphertext);

        return plaintext;
    }

    private IDataProtector ResolveProtector(IntegrationKey key)
    {
        var protector = Protectors.GetOrAdd(key, current => Provider.CreateProtector($"Vuetrack.Connections.{current}.RefreshToken.v1"));

        return protector;
    }
}

public interface IConnectionSecretProtector
{
    string Protect(IntegrationKey key, string plaintext);

    string Unprotect(IntegrationKey key, string ciphertext);
}
