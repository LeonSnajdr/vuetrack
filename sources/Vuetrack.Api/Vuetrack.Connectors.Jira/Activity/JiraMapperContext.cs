using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Jira.Activity;

public sealed record JiraMapperContext(ConnectorKey ConnectorKey, string SiteUrl);
