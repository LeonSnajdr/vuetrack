# Rationale

Reframe suggestions as durable, metadata-driven evidence assembled from noisy connector events. Connector implementations emit small, source-specific activity signals; the engine converts them to bounded candidate intervals, correlates compatible evidence, removes duplicates and low-value events, and persists the resulting suggestions with traceable metadata. The API returns the generated/reloaded suggestions directly, rather than a generation summary.

# Acceptance criteria

- [ ] `GenerateAsync` and `ReloadAsync` return the newly persisted `IReadOnlyList<SuggestionContract>` and `GenerateSuggestionsResultContract`/`ConnectorOutcomeContract` are removed with their call sites and tests updated.
- [ ] Titles are removed from activity signals, engine-only suggestion types, persistence models, API contracts, update requests, repositories, mappers, UI consumers, and test fixtures; display text is resolved from metadata where the UI needs it.
- [ ] Activity-signal links are removed as first-class fields and connector mappers store their source URL in namespaced metadata.
- [ ] A suggestion retains metadata sufficient to explain and compare every contributing signal, including connector key, external ID, normalized interval, and the source metadata; a deterministic merged/canonical metadata view is also defined.
- [ ] Jira emits separate, typed events for worklogs and issue lifecycle changes (for example created, assigned, status changed, moved, and updated), including prior/current values where available.
- [ ] The engine uses signal type and metadata to infer durations for point events, correlate compatible events, merge only eligible adjacent intervals, deduplicate source events, discard weak/noisy candidates, and preserve source provenance.
- [ ] Automated tests cover the illustrated Jira, Teams-call, and GitHub-commit scenario, including a Jira issue event with no explicit end time and a conflicting/noise event.
- [ ] Generate/reload failure behavior is documented and tested: connector failures are logged and do not discard existing data; the returned list contains only successfully persisted suggestions.

# Technical details

Define a stable metadata vocabulary in `Vuetrack.Connectors.Abstractions`, using namespaced keys such as `activity.type`, `display.title`, `source.url`, `work-item.id`, `work-item.status.before`, `work-item.status.after`, `call.id`, `repository`, and `commit.sha`. Keep `ConnectorKey`, `ExternalId`, `DateStarted`, `DateEnded`, `Description`, and metadata on `ActivitySignal`; remove `Title` and `Link`. Connector-specific mappers populate the shared keys and retain source-specific facts under their own prefix.

Replace the title-based correlation fallback in `SuggestionEngine` with an explicit correlation policy. Prefer a shared work-item ID, then a connector-scoped activity/call ID, and otherwise treat events as uncorrelated unless a configured rule allows them to combine. Normalize explicit ranges unchanged. Convert point events into candidate ranges using type-specific policies: worklogs and call start/end events provide strong bounds; commits provide a short bounded focus window; Jira lifecycle events provide a short, lower-confidence window that may extend or bridge a compatible nearby range but must not independently create a long duration. Apply configured limits to prevent one Jira event from spanning unrelated work.

Model provenance explicitly. Enrich `SuggestionSourceModel` (or replace it with a signal-evidence model) with the normalized time range, activity type, and a persisted copy of the raw metadata. Add a deterministic `Metadata` dictionary to `SuggestionModel` for canonical merged values used by comparison and display. Define conflict rules: common values are retained; selected values are resolved by evidence strength and recency; conflicting values remain available on individual source evidence instead of being silently lost. Update Mongo mapping, contracts, mappers, and repository queries accordingly.

Have `BuildAndInsertAsync` return the persisted models/contracts rather than a count. `GenerateAsync` fetches signals, builds and persists candidates, and returns that new list. `ReloadAsync` first fetches; it removes only resettable suggestions backed by connectors that fetched successfully, rebuilds them, and returns its new list. Connector outcome contracts are no longer API payloads; failures remain structured internally for logging/telemetry.

Remove title editing from suggestion update flow. When a suggestion is accepted, its eventual time-entry title/display label should be derived from canonical metadata or user-entered time-entry fields, not copied from an engine-generated title. Update API and UI tests together so the removed field is not serialized or rendered.

Build engine tests as a table of raw signals and expected evidence blocks. Include the test timeline shown in the visualization: Jira status change + Teams call + GitHub commit correlate to `ABC-42`; a Jira issue update for `OPS-7` remains a short/low-confidence candidate or is filtered according to the minimum-evidence rule; duplicate connector events collapse by connector/external ID. Test metadata conflict resolution and that the persisted model preserves each original metadata dictionary.
