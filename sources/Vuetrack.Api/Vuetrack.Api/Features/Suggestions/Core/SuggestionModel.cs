using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core;

[MongoCollection]
public class SuggestionModel : BaseModelMongo
{
    public required string UserId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public required DateTime DateStarted { get; set; }

    public required DateTime DateEnded { get; set; }

    [BsonRepresentation(BsonType.String)]
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;

    public List<SuggestionSourceModel> Sources { get; set; } = [];

    public string? ProposedBackendProjectId { get; set; }

    public string? ProposedBackendActivityId { get; set; }

    public double Confidence { get; set; }

    public required DateTime DateCreated { get; set; }

    public required DateTime DateUpdated { get; set; }
}

public enum SuggestionStatus
{
    Pending,
    Edited,
    Dismissed,
    Confirmed,
}
