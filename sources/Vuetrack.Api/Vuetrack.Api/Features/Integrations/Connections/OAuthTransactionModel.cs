using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

[MongoCollection]
public class OAuthTransactionModel : BaseModelMongo
{
    public required string State { get; init; }

    public required string UserId { get; init; }

    [BsonRepresentation(BsonType.String)]
    public required IntegrationKey Key { get; init; }

    public required string RedirectUri { get; init; }

    public required string CodeVerifier { get; init; }

    public required DateTime DateExpires { get; init; }
}
