using Vuetrack.Api.Features.Integrations;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionEvidenceContract(
    IntegrationKey Key,
    string ExternalId,
    ActivityKind Kind,
    DateTime DateStarted,
    DateTime DateEnded,
    double Weight);
