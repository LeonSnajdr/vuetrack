using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Api.Features.Test;

[MongoCollection]
public class TestModel : BaseModelMongo
{
   public required string Test { get; set; }
}
