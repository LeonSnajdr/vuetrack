using Samhammer.Mongo.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Backends.Timetracking.Connection;

[MongoCollection]
public class TimetrackingConnectionModel : OAuthConnectionModel
{
    public string? ExternalUserId { get; set; }
}
