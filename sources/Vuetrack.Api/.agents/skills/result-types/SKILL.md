---
name: result-types
description: >-
    How to model an operation's known outcomes as a discriminated-union result
    type in this .NET codebase — an abstract record base with sealed record
    cases, consumed with exhaustive switch expressions (like FetchOutcome /
    ValidationOutcome). Use this whenever you write or review a service or
    feature method that can succeed or fail in more than one *expected* way
    (validation, auth, not-found, conflict, rate-limited, …), whenever you catch
    yourself returning null / a bare bool / throwing for an outcome the caller
    is meant to handle, or when naming and placing a `...Result` / `...Outcome`
    type. Reach for it even if the request only says "handle the error", "return
    a failure", or "what should this method return".
---

# Result types

A **result type** makes the *known outcomes* of an operation explicit in its
signature. Instead of a method returning a value and secretly throwing, or
returning `null` and leaving the caller guessing what `null` means, it returns
one of a fixed, named set of cases — and the caller has to say what happens for
each.

In this codebase a result type is a **discriminated union**: an abstract record
base with a sealed record per case, consumed with a `switch` expression. See the
canonical examples in
[`Vuetrack.Connectors.Abstractions/FetchOutcome.cs`](../../../Vuetrack.Connectors.Abstractions/FetchOutcome.cs)
and
[`ValidationOutcome.cs`](../../../Vuetrack.Connectors.Abstractions/ValidationOutcome.cs).
This pattern is not tied to connectors — use it anywhere in the codebase a
method has several expected outcomes.

## The shape

```csharp
public abstract record FetchOutcome;

public sealed record FetchSuccess(IReadOnlyList<ActivitySignal> Signals) : FetchOutcome;

public sealed record FetchAuthFailed(string Reason) : FetchOutcome;

public sealed record FetchRateLimited(TimeSpan RetryAfter) : FetchOutcome;

public sealed record FetchConnectorError(string Message) : FetchOutcome;
```

- The **base** is an `abstract record` — it has no members of its own; it only
  names the family and gives the cases a common return type.
- Each **case** is a `sealed record` deriving from the base. It carries exactly
  the data *that case* needs and nothing more: a success carries its payload, an
  auth failure carries a reason, a rate-limit carries how long to wait. This is
  the whole point — the data a caller can rely on is different per outcome, and
  the type system now reflects that.
- `sealed` because cases are leaves; nothing derives further. `record` because
  these are immutable value carriers, and record equality makes them trivial to
  assert in tests.

The base and all its cases live together in **one file named after the base**
(`FetchOutcome.cs` holds `FetchOutcome` plus every `Fetch*` case). The family is
one unit — reading the file tells you the complete set of outcomes at a glance.

## When to use a result type — and when not

The dividing line is **who is expected to handle the outcome**. A result type is
for outcomes that are a *normal part of the operation's contract* — the caller
is supposed to branch on them. Exceptions are for the abnormal: things no caller
should have to pattern-match because there's nothing sensible to do but fail.

| Reach for a **result type**                                   | **Throw** instead                                        |
| ------------------------------------------------------------- | -------------------------------------------------------- |
| Validation failed (bad input, business rule violated)         | Programmer error (null argument, invalid state, a bug)   |
| Auth/permission denied for this request                       | Infrastructure failure (DB unreachable, socket dropped)  |
| Resource not found / already exists / conflict                | Something you have no recovery path for                  |
| Rate-limited, retry-after, partial success                    | Truly exceptional, "should never happen" conditions      |

Rules of thumb:

- **If you're about to `throw` to signal an outcome the caller will `catch` and
  act on, that outcome belongs in a result type instead.** Exceptions as control
  flow are exactly what this pattern removes.
- **If you're about to return `null` or a bare `bool`, ask what the caller loses.**
  `null` can't say *why*; `bool` can't carry data. If the caller needs to know
  which failure happened or needs data from it, use a result type.
- **Keep the data/repository layer simple — let it throw.** A repository that
  can't reach the database has hit an *exceptional* condition; don't dress that
  up as a result case. Convert genuinely expected failures into result cases in
  the **service layer**, where the business meaning lives. (See how
  `JiraConnectionService` catches `JiraApiException` and returns a
  `JiraConnectContract` describing the failure, rather than letting the exception
  escape to the controller.)

## Naming

`<Concept><Role>` for the base, `<Concept><Case>` for each case:

- **Base** — the concept plus `Result` or `Outcome`. Both are in use:
  `FetchOutcome`, `ValidationOutcome`. Prefer `Result` for "the operation
  produced something", `Outcome` for a pass/fail judgement (a validation is an
  outcome, not a result). Either reads fine; be consistent within a family.
- **Cases** — the concept prefix plus what the case *is*: `FetchSuccess`,
  `FetchAuthFailed`, `FetchRateLimited`, `ValidationValid`, `ValidationInvalid`.
  Name the state, not an error code. The success case is usually
  `<Concept>Success` or `<Concept>Valid`.

The concept prefix matters because these names appear bare in `switch` arms
across the codebase — `FetchAuthFailed` is self-describing at the call site in a
way `AuthFailed` or `Failure` is not.

Result types are a `Container`-family concern (intermediate data a service
produces); they are not `Contract`s. If a result needs to cross the API boundary
it gets mapped to a `Contract` in the controller — see
[dotnet-conventions / file-naming](../dotnet-conventions/references/file-naming.md).

## Consuming and testing

A result type is only worth it if callers handle every case. That — plus how
results map to HTTP responses in controllers, and how to test each case — is in
[references/consuming.md](references/consuming.md). **Read it whenever you write
a `switch` over a result, wire a controller that returns one, or add tests for
one.**
