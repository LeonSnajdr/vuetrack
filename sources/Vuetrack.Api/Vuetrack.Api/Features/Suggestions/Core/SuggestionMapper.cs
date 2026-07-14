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
            TaskId = suggestion.TaskId,
            Comment = suggestion.Comment,
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
            model.TaskId,
            model.ProjectId,
            model.ActivityId,
            model.DateStarted,
            model.DateEnded,
            model.Comment,
            model.Status.ToString(),
            model.Sources.Select(s => new SuggestionSourceContract(s.ConnectorKey, s.ExternalId, s.Link)).ToList(),
            model.Confidence);
    }
}
