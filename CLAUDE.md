# CLAUDE.md

## Project overview

Meeting Room Booking System is an ASP.NET Core MVC application developed as a technical assignment for the Reenbit .NET Internship.

Authenticated users can browse meeting rooms, inspect fixed bookable time slots, and create bookings.

Administrators can additionally manage meeting rooms and bookable time slots.

The central technical requirement is concurrency safety: when multiple requests attempt to book the same slot at effectively the same time, exactly one request must succeed and every losing request must receive a controlled conflict response.

The application also provides real-time schedule updates through SignalR.

## Technology

- .NET 10
- ASP.NET Core MVC
- Razor Views
- minimal vanilla JavaScript
- ASP.NET Core Identity
- cookie authentication
- `User` and `Admin` roles
- Entity Framework Core
- SQL Server for local development
- Azure SQL Database for production
- ASP.NET Core SignalR locally
- Azure SignalR Service for production
- xUnit
- Docker
- Azure App Service

## Solution structure

```text
src/
  MeetingRoomBooking.Domain/
  MeetingRoomBooking.Application/
  MeetingRoomBooking.Infrastructure/
  MeetingRoomBooking.Web/

tests/
  MeetingRoomBooking.UnitTests/
  MeetingRoomBooking.IntegrationTests/
```

## Project responsibilities

### Domain

Contains core entities and domain rules.

Domain must not depend on:

- Application;
- Infrastructure;
- Web;
- Entity Framework Core;
- ASP.NET Core;
- Azure packages.

### Application

Contains application use cases, application-level abstractions, commands,
queries, and DTOs.

Application may depend on Domain.

Application must not depend on Web.

### Infrastructure

Contains:

- Entity Framework Core persistence;
- SQL Server-specific persistence behavior;
- ASP.NET Core Identity persistence;
- implementations of Application abstractions;
- booking concurrency infrastructure.

Infrastructure may depend on Application and Domain.

### Web

Contains:

- MVC controllers;
- Razor Views;
- view models;
- SignalR hubs;
- authentication and authorization configuration;
- dependency injection;
- application startup;
- browser-side JavaScript.

Web may depend on Application and Infrastructure.

## Architecture dependencies

```text
Web ────────────────> Application ─────> Domain
 │                         ▲
 └──────> Infrastructure ──┘
                 │
                 └─────────────────────> Domain
```

Do not introduce dependencies from Domain to Infrastructure or Web.

Do not move business or persistence logic into Razor Views.

## Commands

Restore tools and dependencies:

```bash
dotnet tool restore
dotnet restore
```

Build:

```bash
dotnet build
```

Run all tests:

```bash
dotnet test
```

Run only concurrency integration tests:

```bash
dotnet test tests/MeetingRoomBooking.IntegrationTests \
  --filter FullyQualifiedName~BookingConcurrencyTests
```

Run the web application:

```bash
dotnet run --project src/MeetingRoomBooking.Web
```

Apply Entity Framework Core migrations:

```bash
dotnet ef database update \
  --project src/MeetingRoomBooking.Infrastructure \
  --startup-project src/MeetingRoomBooking.Web
```

Format the solution:

```bash
dotnet format
```

## Authentication and authorization

Authentication uses ASP.NET Core Identity with cookie authentication.

Supported application roles are:

- `User`
- `Admin`

The application seeds the required roles during startup.

Regular authenticated users can:

- browse active meeting rooms;
- view room schedules;
- book available time slots.

Administrators can additionally:

- access administration endpoints;
- create meeting rooms;
- edit meeting rooms;
- activate or deactivate meeting rooms;
- manage bookable time slots.

Authorization must always be enforced on the server.

Hiding an Admin button in the UI is not sufficient protection.

## Admin seed configuration

A local administrator may optionally be seeded through configuration.

The application reads:

```text
SeedAdmin:Email
SeedAdmin:Password
SeedAdmin:DisplayName
```

Either all three values must be provided or none of them.

Do not place administrator credentials in committed configuration files.

For local development, use ASP.NET Core User Secrets.

## Booking concurrency design

Concurrency protection is explicit and database-backed.

Do not replace the existing implementation with a naive:

1. check whether the slot is available;
2. insert the booking;

sequence unless equivalent explicit synchronization is preserved.

### Transactional locking

Booking creation uses an explicit SQL Server transaction at the
`Read Committed` isolation level.

The target meeting room is read using SQL Server locking hints:

```sql
UPDLOCK, HOLDLOCK
```

This deliberately serializes booking writers for the same meeting room.

While the lock is held, the booking operation:

1. validates the requested time slots;
2. checks whether any selected slot is already booked;
3. creates the booking;
4. creates its slot associations;
5. saves the changes;
6. commits the transaction.

Competing booking requests for the same room wait for the same lock.

After the winning transaction commits, the next request performs its
availability checks against the newly committed state.

If any requested slot has already been booked, the request returns a
controlled booking conflict.

### Database invariant

The database contains a unique index:

```text
UX_BookingTimeSlots_TimeSlotId
```

on the booking/time-slot association.

This guarantees at the database level that one `TimeSlot` cannot belong
to multiple bookings.

The unique index is a final safeguard in addition to the room locking
protocol.

### SQL Server conflict translation

SQL Server unique constraint errors:

```text
2601
2627
```

are translated into a booking conflict only when the failure refers to the
specific booking time-slot uniqueness invariant.

Do not catch every `DbUpdateException` and convert it to HTTP 409.

Unrelated database failures must remain visible as actual failures.

### Multi-slot bookings

Multi-slot booking is atomic.

A booking request must create either:

- the entire requested booking;

or:

- nothing.

Never persist a subset of requested slots after a conflict.

### Trade-off

The concurrency lock is acquired at meeting-room level rather than individual
time-slot level.

This intentionally favors correctness and simplicity.

A consequence is that simultaneous bookings for different free slots in the
same meeting room are serialized for the duration of the transaction.

The lock is database-backed rather than process-local, so it remains effective
when multiple application instances use the same SQL Server database.

## Booking HTTP behavior

Successful booking requests redirect to the affected room schedule.

A booking conflict returns:

```text
HTTP 409 Conflict
```

and the schedule page displays a user-readable conflict explanation.

Concurrency failures must never appear as an unhandled HTTP 500 response.

## Testing requirements

Use xUnit.

The repository contains integration tests for:

- authentication and authorization behavior;
- normal booking flow;
- concurrent requests for the same slot;
- concurrent partially overlapping multi-slot bookings.

Concurrency-sensitive behavior must be tested against SQL Server rather than
an in-memory database because the implementation relies on SQL Server
transactions, locking hints, and unique constraints.

### Concurrency test invariant

The primary concurrency test sends multiple simultaneous HTTP booking requests
for the same slot.

It must verify:

- exactly one request succeeds;
- every losing request receives HTTP 409;
- exactly one booking is persisted;
- the persisted booking belongs to the winning user;
- only the winner's slot associations remain in the database.

Do not weaken these assertions.

### Integration test realism

Concurrency integration tests use:

- an isolated SQL Server container;
- the real ASP.NET Core application pipeline;
- HTTP requests;
- authentication cookies;
- antiforgery tokens.

Keep concurrency tests deterministic and runnable by a reviewer with Docker
available.

## Real-time schedule updates

The application uses ASP.NET Core SignalR.

The room schedule hub is exposed at:

```text
/hubs/room-schedule
```

Clients viewing a room schedule subscribe to updates for that room.

After booking state changes, affected viewers should receive an immediate
update without manually refreshing the page.

Local development uses ASP.NET Core SignalR.

The deployed production environment is intended to use Azure SignalR Service.

Do not make correctness depend on SignalR.

The database and booking service remain the source of truth; real-time updates
are a presentation concern.

## Entity Framework Core

Use asynchronous Entity Framework Core APIs for I/O.

Pass `CancellationToken` through application and persistence operations where
appropriate.

Keep database-specific behavior in Infrastructure.

Do not introduce a generic repository abstraction merely to wrap basic
`DbSet` or `DbContext` methods.

Migrations belong to the Infrastructure persistence project.

## Code quality

- Nullable reference types remain enabled.
- Prefer descriptive names over abbreviations.
- Keep controllers thin.
- Keep business logic outside Razor Views.
- Keep persistence details outside Domain.
- Use asynchronous APIs for I/O.
- Avoid unnecessary abstractions.
- Avoid speculative design patterns.
- Investigate compiler warnings rather than ignoring them.
- Add comments only when they explain a non-obvious invariant, trade-off, or
  implementation constraint.
- Preserve existing HTTP status semantics when refactoring.
- Preserve concurrency invariants when changing booking code.

## UI guidelines

The UI uses Razor Views and minimal vanilla JavaScript.

Do not introduce a JavaScript framework unless there is a clear project
requirement.

Server-side authorization must remain authoritative regardless of what UI
elements are visible.

SignalR client code should gracefully handle temporary connection failures and
allow reconnecting after the backend becomes available again.

## Security

Never commit:

- passwords;
- connection strings containing credentials;
- API keys;
- Azure credentials;
- `.env`;
- ASP.NET Core User Secrets;
- authentication cookies;
- access tokens;
- generated build artifacts.

Use:

- ASP.NET Core User Secrets for local sensitive application configuration;
- environment variables or Azure configuration for deployed environments.

Do not log:

- passwords;
- authentication cookies;
- access tokens;
- complete connection strings.

## Development workflow

Before making a substantial change:

1. inspect the relevant projects and dependencies;
2. identify the layer in which the change belongs;
3. explain non-obvious architectural choices;
4. keep the change focused;
5. build the solution;
6. run relevant tests;
7. verify formatting when appropriate;
8. summarize the result and unresolved risks.

For concurrency-sensitive changes, always run the concurrency integration
tests.

For authentication or authorization changes, run the relevant access and
booking flow integration tests.

Do not create commits unless explicitly requested.

## Git conventions

Commits must be small, atomic, and explain what changed.

Prefer imperative commit subjects, for example:

```text
Add room management domain model
Prevent concurrent booking conflicts
Test overlapping slot bookings
Broadcast booking updates through SignalR
Document project architecture
```

Do not combine unrelated feature work, formatting, refactoring, and
documentation into one commit.

## AI-assisted development

Claude Code is used as an AI development assistant for this project.

This file serves as the persistent project context supplied to the assistant.

When proposing or implementing changes, the assistant must respect:

- the solution dependency boundaries;
- the explicit SQL Server concurrency protocol;
- the database uniqueness invariant;
- HTTP 409 conflict behavior;
- authentication and role authorization rules;
- the integration testing strategy;
- SignalR responsibilities;
- security requirements;
- atomic Git workflow.

AI-generated suggestions must be treated as proposed engineering changes,
not as authority over the existing architecture.

Changes should be validated through source inspection, build output, and
automated tests before being accepted.