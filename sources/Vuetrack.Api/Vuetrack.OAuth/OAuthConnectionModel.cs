using Samhammer.Mongo.Abstractions;

namespace Vuetrack.OAuth;

public abstract class OAuthConnectionModel : BaseModelMongo
{
    public required string UserId { get; set; }

    public required string AuthMode { get; set; }

    public required string EncryptedRefreshToken { get; set; }

    public bool Enabled { get; set; } = true;
}
