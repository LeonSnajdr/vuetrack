using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

[MongoCollection]
public class ConnectionModel : BaseModelMongo
{
    public required string UserId { get; init; }

    [BsonRepresentation(BsonType.String)]
    public required IntegrationKey Key { get; init; }

    public required string EncryptedRefreshToken { get; set; }

    public Dictionary<string, string> Attributes { get; set; } = [];
}
