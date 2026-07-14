using Samhammer.Mongo.Abstractions;

namespace Vuetrack.OAuth;

/// <summary>
/// Shared fields for a stored OAuth2 3LO connection. Concrete platforms subclass this and carry
/// the <c>[MongoCollection]</c> attribute so each keeps its own collection.
/// </summary>
public abstract class OAuthConnectionModel : BaseModelMongo
{
    public required string UserId { get; set; }

    public required string AuthMode { get; set; }

    public required string EncryptedRefreshToken { get; set; }

    public bool Enabled { get; set; } = true;
}
