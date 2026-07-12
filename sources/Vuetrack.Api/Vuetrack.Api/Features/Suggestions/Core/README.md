# Suggestions

Turns connected sources (Jira today, more connectors later) into proposed time
entries the user can review, edit, or dismiss.

## Generation flow

`POST /api/v1/suggestions/generate` (`SuggestionService.GenerateAsync`):

1. For every descriptor in `IConnectorRegistry.Descriptors` (or the subset named
   in `GenerateSuggestionsRequestContract.ConnectorKeys`), resolve the matching
   `IConnectorContextInitializer` by `ConnectorKey` and call
   `TryInitializeAsync(userId, ct)`. This establishes the connector's ambient,
   per-user connection context (e.g. Jira's OAuth token) without the feature
   knowing anything connector-specific.
2. If initialization fails (`false`), record a `NotConnected` outcome and skip.
   Otherwise resolve the `IConnector` from the registry and call `FetchAsync`.
3. Match the returned `ActivityFetchResult` exhaustively into a
   `ConnectorOutcomeContract` (`Success`, `NotConnected`, `AuthFailed`,
   `RateLimited`, `Error`). A connector throwing an unexpected exception is
   caught and recorded as `Error` too - one failing source never fails the
   whole generation request.
4. All collected `ActivitySignal`s (across every connector) are passed to
   `ISuggestionEngine.Build(signals, from, to)` - the pure engine in
   `Vuetrack.Suggestions.Engine` that groups/merges them into `TimeSuggestion`s.
5. Each `TimeSuggestion` is persisted idempotently (see below) as a new
   `SuggestionModel` with `Status = Pending`.
6. The response is a `GenerateSuggestionsResultContract`: how many suggestions
   were newly generated, plus the per-connector outcome list so the UI can
   show e.g. "Jira: 4 signals", "Outlook: auth failed".

## Engine options (`SuggestionEngineOptions`, bound from `appsettings.json`)

| Option | Default | Effect |
| --- | --- | --- |
| `MergeGap` | 15 min | Signals in the same correlation group with a gap (or overlap) within this window are merged into one block. A gap larger than this starts a new block. |
| `RoundTo` | 5 min | Block start rounds down, end rounds up, to this granularity. |
| `MinimumBlock` | 5 min | Blocks shorter than this *after* rounding are dropped entirely (not extended). |
| `DefaultPointDuration` | 15 min | Duration assumed for a signal with no `End` (e.g. an issue-updated event rather than a worklog). |
| `CorrelationMetadataKey` | `correlationId` | The `ActivitySignal.Metadata` key connectors can set to control grouping (see below). |

## Idempotency policy

Regenerating over a range you've already generated must not duplicate
suggestions or clobber what the user has already done with them. The rule:

- For every `TimeSuggestion` the engine produces, check whether **any** of its
  contributing sources (`ConnectorKey` + `ExternalId`) already appears on a
  persisted `SuggestionModel` for that user (`ISuggestionRepository.ExistsBySourceAsync`).
  If so, skip it - the engine is deterministic, so the same input range
  reproduces the same grouping, making "any source already exists" a reliable
  per-suggestion dedup signal.
- Suggestions the user has already acted on (`Edited`, `Dismissed`,
  `Confirmed`) are never read back into the generation loop and never
  overwritten - only genuinely new suggestions are inserted, always as
  `Pending`.

## Extending grouping: the `correlationId` metadata contract

The engine groups signals into blocks by a *correlation key*: it reads
`ActivitySignal.Metadata[CorrelationMetadataKey]` (default key
`"correlationId"`) if present, and falls back to `"{ConnectorKey}|{Title}"`
otherwise. Connectors that want tighter control over grouping (e.g. keying
worklogs and issue-updated events on the same Jira issue together even if
their titles differ slightly) should populate `Metadata["correlationId"]`
with a stable identifier for "the same piece of work" - the engine itself
stays connector-agnostic and never inspects any other metadata key.
