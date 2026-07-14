using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Backends.Timetracking.Connection;

[MongoCollection]
public class TimetrackingConnectionModel : BaseModelMongo
{
    public required string UserId { get; set; }

    public required string AuthMode { get; set; }

    public required string EncryptedRefreshToken { get; set; }

    public string? ExternalUserId { get; set; }

    public bool Enabled { get; set; } = true;
}
