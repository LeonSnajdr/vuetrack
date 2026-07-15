using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Engine;

namespace Vuetrack.Api.Features.Suggestions.Core;

public static class SuggestionMapper
{
    public static SuggestionModel ToModel(this SuggestionEngineResult result, string userId, DateTime now)
    {
        var sources = result.Sources.Select(ToEvidenceModel).ToList();

        return new SuggestionModel
        {
            UserId = userId,
            TaskId = result.TaskId,
            Comment = result.Comment,
            DateStarted = result.DateStarted,
            DateEnded = result.DateEnded,
            Status = SuggestionStatus.Pending,
            Sources = sources,
            Confidence = result.Confidence,
            DateCreated = now,
            DateUpdated = now,
        };
    }

    public static SuggestionContract ToContract(this SuggestionModel model)
    {
        var sources = model.Sources.Select(ToEvidenceContract).ToList();

        return new SuggestionContract(
            model.Id,
            model.TaskId,
            model.ProjectId,
            model.ActivityId,
            model.DateStarted,
            model.DateEnded,
            model.Comment,
            model.Status.ToString(),
            sources,
            model.Confidence);
    }

    private static SuggestionEvidenceModel ToEvidenceModel(SuggestionEngineEvidence evidence)
    {
        return new SuggestionEvidenceModel
        {
            ConnectorKey = evidence.ConnectorKey,
            ExternalId = evidence.ExternalId,
            Kind = evidence.Kind,
            DateStarted = evidence.DateStarted,
            DateEnded = evidence.DateEnded,
            Confidence = evidence.Confidence,
        };
    }

    private static SuggestionEvidenceContract ToEvidenceContract(SuggestionEvidenceModel model)
    {
        return new SuggestionEvidenceContract(
            model.ConnectorKey,
            model.ExternalId,
            model.Kind,
            model.DateStarted,
            model.DateEnded,
            model.Confidence);
    }
}
