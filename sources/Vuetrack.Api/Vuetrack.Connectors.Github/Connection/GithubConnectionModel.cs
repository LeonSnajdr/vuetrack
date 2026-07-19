using Samhammer.Mongo.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Github.Connection;

[MongoCollection]
public class GithubConnectionModel : OAuthConnectionModel
{
    public required string Login { get; init; }
}
