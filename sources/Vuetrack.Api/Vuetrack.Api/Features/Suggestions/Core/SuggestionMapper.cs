using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Engine;

namespace Vuetrack.Api.Features.Suggestions.Core;

public static class SuggestionMapper
{
    public static SuggestionModel ToModel(this TimeSuggestion suggestion, string userId, DateTime now)
    {
        return new SuggestionModel
        {
            UserId = userId,
            Title = suggestion.Title,
            Description = suggestion.Description,
            DateStarted = suggestion.DateStarted,
            DateEnded = suggestion.DateEnded,
            Status = SuggestionStatus.Pending,
            Sources = suggestion.Sources.Select(s => new SuggestionSourceModel
            {
                ConnectorKey = s.ConnectorKey,
                ExternalId = s.ExternalId,
                Link = s.Link,
            }).ToList(),
            Confidence = suggestion.Confidence,
            DateCreated = now,
            DateUpdated = now,
        };
    }

    public static SuggestionContract ToContract(this SuggestionModel model)
    {
        return new SuggestionContract(
            model.Id,
            model.Title,
            model.Description,
            model.DateStarted,
            model.DateEnded,
            model.Status.ToString(),
            model.Sources.Select(s => new SuggestionSourceContract(s.ConnectorKey, s.ExternalId, s.Link)).ToList(),
            model.Confidence,
            model.ProposedBackendProjectId,
            model.ProposedBackendActivityId);
    }
}
