using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Connectors.Jira.Models;

[MongoCollection]
public class JiraConnectionModel : BaseModelMongo
{
    public required string UserId { get; set; }

    public required string SiteUrl { get; set; }

    public required string CloudId { get; set; }

    public required string AuthMode { get; set; }

    public required string EncryptedRefreshToken { get; set; }

    public bool Enabled { get; set; } = true;
}
