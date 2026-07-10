# Vuetrack.Connectors.Jira

The Jira suggestion connector. It pulls a user's recent Jira activity (worklogs, plus a
weaker "issues you touched" fallback) for a time range and returns normalized
`ActivitySignal`s that the suggestion engine consumes. It is the reference implementation
of the connector platform seam in `Vuetrack.Connectors.Abstractions`.

Targets `net10.0`, consistent with the rest of the solution. Result types (`FetchResult`,
`ValidationOutcome`) are sealed-record hierarchies matched with pattern matching; they live
only transiently between produce-and-match and never appear in a Mongo document, a DTO, or
the API contract.

## Configuration

Bound from the `JiraOptions` config section (see `appsettings.json`). All values here are
non-secret defaults; real secrets come from environment/config substitution.

| Key | Meaning | Default |
| --- | --- | --- |
| `IdentityBaseUrl` | Atlassian OAuth host | `https://auth.atlassian.com` |
| `ApiBaseUrl` | Atlassian API gateway | `https://api.atlassian.com` |
| `ClientId` | OAuth app client id | `""` (set via env) |
| `ClientSecret` | OAuth app client secret | `""` (set via env) |
| `Scopes` | OAuth scopes | `read:jira-work read:jira-user offline_access` |
| `TimeoutSeconds` | Per-request HTTP timeout | `30` |
| `PageSize` | Jira search page size | `50` |
| `MaxPages` | Hard cap on pages per query | `20` |

`ClientId`/`ClientSecret` identify one Atlassian OAuth 2.0 (3LO) app for the whole
deployment. Create it at <https://developer.atlassian.com/console/myapps/>, enable the Jira
API, add the scopes above (`offline_access` is required for a refresh token), and register
the callback URL your frontend posts back to.

## How connecting works (OAuth 2.0 3LO)

1. `GET /api/v1/connectors/jira/authorize?redirectUri=...` returns the Atlassian consent URL
   and a `state` value. The UI sends the user there.
2. Atlassian redirects back with a `code`. The UI posts it to
   `POST /api/v1/connectors/jira/callback` (`{ code, state, redirectUri }`, validated by
   FluentValidation).
3. The connector exchanges the code, resolves the accessible Jira site (`cloudId` + URL),
   calls `myself` to validate, and — on success — stores the connection.

The OAuth token endpoint calls (authorize URL, code exchange, refresh) use
**`Duende.IdentityModel`** (`RequestUrl` / `RequestAuthorizationCodeTokenAsync` /
`RequestRefreshTokenAsync`) rather than hand-rolled HTTP.

Only the encrypted refresh token is persisted (`JiraConnectionModel`), via `ISecretProtector`
(ASP.NET Data Protection, local key ring — no external key service). Access tokens live in
memory only. No token, code, or secret is written to a log, a URL, or the API response.

## Scoped connection + auth

The live connection for the current operation (access token + `cloudId` + `siteUrl`) is a
`JiraConnectionContainer` held on `IJiraConnectionAccessor`, a **scoped** service (one instance per
HTTP request) — **not** threaded through the connector API. `JiraApiClient` reads it to build the
`cloudId`-addressed URL *and* to attach the `Bearer` header on every request, so no delegating
handler is involved.

The connection reaches the accessor two ways: a fetch flow calls
`JiraConnectionContextFactory.CreateAsync`, which resolves the connection and publishes it on the
scoped accessor itself; the connect flow (whose token is freshly exchanged and not yet persisted)
assigns `accessor.Current` directly in `JiraConnectionService`. Because the accessor is a scoped
reference (not an `AsyncLocal`), a callee setting it is visible to the caller. A background/hosted
service driving a fetch must first open a DI scope; nothing does that today.

## Fetching

`JiraConnectionContextFactory.CreateAsync` resolves a stored connection into a `JiraConnection`
(cached access token; on miss, decrypt refresh token → refresh → persist any rotated refresh
token), with `Evict(userId)` to drop the cache on 401/403. `JiraConnector.FetchAsync` then
resolves the account (`GET /myself`), finds worklogs authored by the user in range
(`/search/jql` then `GET /issue/{key}/worklog`), finds fallback issue activity (`/search/jql`),
and maps everything to `ActivitySignal`s (`JiraActivityMapper`), deduping so a worklog supersedes
the weaker issue signal for the same issue.

HTTP failures never throw out of the connector: 401/403 map to `AuthFailed`, 429 to
`RateLimited` (honoring `Retry-After`), anything else to `ConnectorError`.

## Project layout

Types are organized into role folders: `ApiClients/` (HTTP clients — `JiraApiClient`,
`JiraOAuthApiClient`), `Services/` (`JiraActivityMapper`, `SecretProtector`,
`JiraConnectionContextFactory`), `Contracts/` (`Response` records deserialized from the Jira
API), `Models/` (the Mongo `JiraConnectionModel`), `Repositories/`, `Configuration/`
(`JiraOptions`), `Exceptions/`, and `Internal/` (parsing helpers). `JiraConnector` (the
`ISuggestionConnector` entry point) sits at the project root.

Naming convention: `Model` = database model only; `Response` = data coming from an external
API; `Request` = data going to an external API; HTTP clients carry the `ApiClient` suffix and
are treated as repositories (interface + DI attribute).

## Registration

Registration is mostly attribute-driven via the Samhammer packages already wired in `Program.cs`
(`ResolveOptions` + `ResolveDependencies`):

- `[Option]` on `JiraOptions` binds the `JiraOptions` config section.
- `[Inject]` on clients, services, and the repository registers each to its matching interface;
  `[Inject(Target.Matching, ServiceLifetime.Scoped)]` on `JiraConnectionAccessor` scopes the
  connection holder to the request.
- `[InjectAs(typeof(ISuggestionConnector))]` on `JiraConnector` registers it as a plain (non-keyed)
  `ISuggestionConnector`. `ConnectorRegistry` collects `IEnumerable<ISuggestionConnector>` and
  resolves by `Descriptor.Key`, so `GET /api/v1/connectors` picks up every connector automatically.

The one piece of explicit wiring is `AddJiraConnectorHttpClients()` (called in `Program.cs`): it
registers `JiraApiClient` as a **typed `HttpClient`** (which is why `JiraApiClient` does *not* carry
`[Inject]`); the client attaches the `Bearer` header itself, so no delegating handler is needed.
`JiraOAuthApiClient` stays `[Inject]` on the default `HttpClient` (the token endpoint must not
receive a Bearer header).

## Adding the next connector

Copy this project as the template:

1. New project `Vuetrack.Connectors.<Name>` referencing `Vuetrack.Connectors.Abstractions`.
2. Implement `ISuggestionConnector` with a `ConnectorDescriptor` (unique `Key`,
   capabilities, config schema); annotate it `[InjectAs(typeof(ISuggestionConnector))]`.
3. Add a thin `*ApiClient` (annotated `[Inject]`, injecting `HttpClient`) and a pure mapper to
   `ActivitySignal`.
4. If it stores secrets, reuse the Data Protection pattern (`ISecretProtector`) — never
   store plaintext.
5. Annotate options with `[Option]` and every service/client/repository with `[Inject]`; no
   manual `IServiceCollection` wiring is needed.
6. Reference the new project from `Vuetrack.Api`. `ResolveDependencies`/`ResolveOptions` scan it
   automatically — no `Program.cs` change required.
7. Unit-test the mapper and the client (mocked `HttpMessageHandler`) as in
   `Vuetrack.Connectors.Jira.Tests`.
