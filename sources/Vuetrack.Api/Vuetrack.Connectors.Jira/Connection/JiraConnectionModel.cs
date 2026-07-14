using Samhammer.Mongo.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Jira.Connection;

[MongoCollection]
public class JiraConnectionModel : OAuthConnectionModel
{
    public required string SiteUrl { get; set; }

    public required string CloudId { get; set; }
}
