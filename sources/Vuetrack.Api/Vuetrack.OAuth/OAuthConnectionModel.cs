using Samhammer.Mongo.Abstractions;

namespace Vuetrack.OAuth;

public abstract class OAuthConnectionModel : BaseModelMongo
{
    public required string UserId { get; init; }

    public required string AuthMode { get; init; }

    public required string EncryptedRefreshToken { get; set; }

    public bool Enabled { get; init; } = true;
}
