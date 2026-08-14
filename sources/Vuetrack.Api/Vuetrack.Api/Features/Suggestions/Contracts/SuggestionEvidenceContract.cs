using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Features.Suggestions.Contracts;

public sealed record SuggestionEvidenceContract(
    IntegrationKey Key,
    string ExternalId,
    ActivityKind Kind,
    DateTime DateStarted,
    DateTime DateEnded,
    double Weight);
