using Samhammer.Mongo.Abstractions;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

[MongoCollection]
public class ConnectionModel : BaseModelMongo
{
    public required string UserId { get; init; }

    public required IntegrationKey Key { get; init; }

    public required string EncryptedRefreshToken { get; set; }

    public Dictionary<string, string> Attributes { get; set; } = [];
}
