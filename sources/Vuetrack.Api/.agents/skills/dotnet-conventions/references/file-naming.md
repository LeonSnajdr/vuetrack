# File & type naming

The suffix a class carries tells you (and every future reader) what role it
plays and which layer it belongs to. The **file name matches the type name**, so
the suffix is visible in the file tree too: `UserContract` lives in
`UserContract.cs`.

## Why suffixes matter

In a layered app the same concept — say, a user — shows up in several shapes:
what the database stores, what the API hands back, what an external service
expects. If they were all just called `User`, you couldn't tell them apart, and
a reader would have to open the file and read the properties to know whether
they're looking at a DB row or an HTTP payload.

The suffix removes that guesswork. `UserModel` is a database row; `UserContract`
is what the API returns; a `WeatherResponse` is what came back from an external
service. The name alone tells you the role and the layer, so imports, folder
placement, and mapping direction all become obvious at a glance.

**The suffix names the role, not the shape.** Two classes with identical
properties still get different suffixes if they sit at different points in the
flow — that difference is the whole point.

## The data flow

Data moves through the layers like this, and each hop has its own suffix:

```
                    ┌─────────────────────────── your API surface ──────────────────────────┐
   API clients  ──▶ │  Filter (query in)                              Contract (data out)    │
                    └──────────┬────────────────────────────────────────────▲────────────────┘
                               │                                             │
                               ▼          business logic maps  Model ──▶ Contract  directly
              Model (persist)  │                                             ▲
   Database   ◀────────────────┘                              External API ◀─┘  Request / Response
                                                                                 (talk to outside)

   Container ── intermediate data a service produces that is neither a Model nor a Contract
```

- Inbound API queries arrive as a **Filter** and can travel all the way down to
  the database query.
- The database layer speaks in **Model**s, and the API's public shapes are
  **Contract**s. A read **Contract** is mapped *out of* a `Model` and returned
  (usual mapping: **`Model → Contract`**, nothing in between); a write
  **Contract** (`...CreateContract` / `...UpdateContract`, always separate) is
  what the controller *receives*.
- A **Container** is *not* part of that path. It's intermediate data a service
  generates that matches neither a Model nor a Contract.
- When your app calls *another* service, it sends a **Request** and gets back a
  **Response**.

## The suffixes

| Suffix        | Role                                                          | Lives / flows                                   | Example file         |
| ------------- | ------------------------------------------------------------- | ----------------------------------------------- | -------------------- |
| **Contract**  | The API's public shapes — returned (read) or received to create/update (write) | API boundary, both directions  | `UserContract.cs`, `UserCreateContract.cs` |
| **Filter**    | Filter/query options received by the API                      | Enters at the API, may reach through all layers | `UserFilter.cs`      |
| **Model**     | Everything saved in the database                              | Persistence layer                               | `UserModel.cs`       |
| **Container** | Intermediate data a service produces that is neither a Model nor a Contract | Application / service layer        | `PasswordStrengthContainer.cs` |
| **Request**   | A container **sent to** an external API                       | Outbound to a third-party service               | `WeatherRequest.cs`  |
| **Response**  | A container **received from** an external API                 | Inbound from a third-party service              | `WeatherResponse.cs` |

### Contract — the API's public shapes

A `Contract` is a public, serialized shape that crosses your API boundary — it's
your API's contract with the outside world (hence the name). Contracts come in
two flavours, split by direction:

- **Read contract** — what the API **returns**. Mapped *out of* a `Model` (or a
  `Container`) and handed to the caller. This is the plain `Contract`.
- **Write contract** — what the controller **receives** to create or update
  something. Create and update are **always kept separate**, never a shared
  type: the fields a client may set on creation rarely match what it may change
  later, and folding them together forces nullable/ignored properties that hide
  the real contract. Name the operation into the type — `UserCreateContract`,
  `UserUpdateContract`.

```csharp
// Read: mapped out of a UserModel and returned.
public class UserContract
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
}

// Write: received by the controller. Separate types per operation.
public class UserCreateContract
{
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserUpdateContract
{
    public string DisplayName { get; set; }
}
```

### Filter — query options coming in

Options a client passes to narrow or shape a query are a `Filter`. A `Filter` is
special in that it can be **reached through all layers** — it enters at the API
and may be handed down as far as the database query, so it isn't confined to the
API boundary the way a `Contract` is.

```csharp
public class UserFilter
{
    public string? NameContains { get; set; }
    public bool? IsActive { get; set; }
}
```

### Model — what the database stores

A `Model` is a persistence shape: it mirrors what's stored in the database.
Reserve this suffix for types that the data layer reads and writes.

```csharp
public class UserModel
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
}
```

### Container — intermediate service state

Business logic works with `Model`s and `Contract`s directly — a `Container` is
**not** a layer that data routinely passes through. When serving a request the
usual mapping is `Model → Contract` with nothing in between.

Reach for a `Container` only when a service produces intermediate data that
matches **neither** a stored `Model` **nor** a returned `Contract` — some
computed or aggregated shape that exists purely inside the application. If the
shape corresponds to something in the database, it's a `Model`; if it's what the
API hands back, it's a `Contract`. The `Container` is the leftover case.

```csharp
// Produced by a service that scores a password; never stored, never returned as-is.
public class PasswordStrengthContainer
{
    public int Score { get; set; }
    public bool MeetsPolicy { get; set; }
    public IReadOnlyList<string> FailedRules { get; set; }
}
```

### Request / Response — talking to an external API

When your app calls *another* service, the two are `Container`s specialized for
that conversation:

- **Request** — the container you **send to** the external API.
- **Response** — the container you **receive from** the external API.

The direction is from *your* app's point of view: you send a `Request`, they
send back a `Response`.

```csharp
public class WeatherRequest
{
    public string City { get; set; }
    public string Units { get; set; }
}

public class WeatherResponse
{
    public decimal TemperatureCelsius { get; set; }
    public string Condition { get; set; }
}
```

## Choosing the right suffix

Ask what **role** the type plays, in this order:

1. Does it cross the boundary to an **external** service? → `Request` (you send
   it) or `Response` (you receive it).
2. Is it a public shape crossing **your** API boundary? → `Contract` — a read
   `Contract` if returned, a separate `...CreateContract` / `...UpdateContract`
   if received to create or update.
3. Is it filter/query options coming **into** your API? → `Filter`.
4. Is it what the **database** stores? → `Model`.
5. None of the above — it's intermediate data a service produced that fits
   neither a Model nor a Contract? → `Container`.

If a type seems to fit two suffixes, that's usually a sign it's doing two jobs —
prefer splitting it so each layer keeps its own shape and mapping stays explicit.

## Applying it

- The **file name matches the type name**: `UserContract` lives in
  `UserContract.cs`. This is standard C# and it means the suffix is visible in
  the file tree, not just in code.
- Prefix with the concept, suffix with the role: `<Concept><Suffix>` —
  `OrderContract`, `OrderModel`, `OrderContainer`.
- When adding a new data-carrying type, pick the suffix from the flow above
  *before* naming it, so its layer is unambiguous from the first commit.
