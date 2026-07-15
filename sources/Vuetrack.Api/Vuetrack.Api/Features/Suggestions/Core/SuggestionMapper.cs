using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Engine;

namespace Vuetrack.Api.Features.Suggestions.Core;

public static class SuggestionMapper
{
    public static SuggestionModel ToModel(this TimeSuggestion suggestion, string userId, DateTime now)
    {
        var canonical = SuggestionMetadataJson.Serialize(suggestion.CanonicalMetadata);
        var sources = suggestion.Sources.Select(ToEvidenceModel).ToList();

        return new SuggestionModel
        {
            UserId = userId,
            TaskId = suggestion.TaskId,
            ProjectName = suggestion.ProjectName,
            Comment = suggestion.Comment,
            DateStarted = suggestion.DateStarted,
            DateEnded = suggestion.DateEnded,
            Status = SuggestionStatus.Pending,
            MetadataJson = canonical,
            Sources = sources,
            Confidence = suggestion.Confidence,
            DateCreated = now,
            DateUpdated = now,
        };
    }

    public static SuggestionContract ToContract(this SuggestionModel model)
    {
        var metadata = SuggestionMetadataJson.Deserialize(model.MetadataJson);
        var sources = model.Sources.Select(ToEvidenceContract).ToList();

        return new SuggestionContract(
            model.Id,
            model.TaskId,
            model.ProjectId,
            model.ProjectName,
            model.ActivityId,
            model.DateStarted,
            model.DateEnded,
            model.Comment,
            model.Status.ToString(),
            metadata,
            sources,
            model.Confidence);
    }

    private static SuggestionEvidenceModel ToEvidenceModel(SuggestionEvidence evidence)
    {
        var metadata = SuggestionMetadataJson.Serialize(evidence.Metadata);

        return new SuggestionEvidenceModel
        {
            ConnectorKey = evidence.ConnectorKey,
            ExternalId = evidence.ExternalId,
            Kind = evidence.Kind,
            DateStarted = evidence.DateStarted,
            DateEnded = evidence.DateEnded,
            Weight = evidence.Weight,
            MetadataJson = metadata,
        };
    }

    private static SuggestionEvidenceContract ToEvidenceContract(SuggestionEvidenceModel model)
    {
        var metadata = SuggestionMetadataJson.Deserialize(model.MetadataJson);

        return new SuggestionEvidenceContract(
            model.ConnectorKey,
            model.ExternalId,
            model.Kind,
            model.DateStarted,
            model.DateEnded,
            model.Weight,
            metadata);
    }
}
