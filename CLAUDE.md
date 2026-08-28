# CLAUDE.md

## Project overview

Meeting Room Booking System is an ASP.NET Core MVC application that allows authenticated users to view meeting-room schedules and book fixed time slots.

The central technical requirement is concurrency safety: if multiple requests attempt to book the same slot simultaneously, exactly one request must succeed. All others must receive a clear conflict response.

## Technology

* .NET 10
* ASP.NET Core MVC and Razor Views
* Minimal vanilla JavaScript
* ASP.NET Core Identity
* Cookie authentication
* `User` and `Admin` roles
* Entity Framework Core
* SQL Server locally
* Azure SQL Database in production
* ASP.NET Core SignalR locally
* Azure SignalR Service in production
* xUnit
* Docker
* Azure App Service

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

Contains entities, value objects, domain rules, and domain-specific exceptions.

Domain must not depend on Application, Infrastructure, Web, Entity Framework Core, ASP.NET Core, or Azure packages.

### Application

Contains use cases, application services, commands, queries, DTOs, and abstractions required by use cases.

Application may depend on Domain but must not depend on Web.

### Infrastructure

Contains Entity Framework Core, SQL Server persistence, Identity persistence, and implementations of Application abstractions.

Infrastructure may depend on Application and Domain.

### Web

Contains MVC controllers, Razor Views, view models, SignalR hubs, authentication configuration, dependency injection, and application startup.

Web may depend on Application and Infrastructure.

## Commands

Restore and build:

```bash
dotnet restore
dotnet build
```

Run all tests:

```bash
dotnet test
```

Run the web application:

```bash
dotnet run --project src/MeetingRoomBooking.Web
```

## Concurrency requirements

Concurrency protection must be explicit and deliberate.

Never implement booking as an unprotected sequence of:

1. Check whether the slot is available.
2. Insert a booking.

The final implementation must include a database-backed guarantee or another explicit concurrency mechanism ensuring that:

* exactly one concurrent request succeeds;
* competing requests receive a controlled conflict result;
* no booking is silently overwritten;
* a concurrency conflict does not become an unhandled server error;
* the database contains exactly one booking for the slot.

Before implementing concurrency control, explain the selected mechanism and its trade-offs.

## Testing requirements

* Use xUnit.
* Unit-test business rules independently where practical.
* Use integration tests for persistence and HTTP behaviour.
* Include an automated test that sends multiple simultaneous booking requests for the same slot.
* Assert that exactly one request succeeds.
* Assert that exactly one booking exists afterward.
* Tests must be deterministic and runnable by a reviewer.

Do not add meaningless tests that only assert constants or framework behaviour.

## Real-time requirements

When a booking status changes, connected viewers of the affected resource schedule must receive an immediate SignalR update without refreshing the page.

Use ASP.NET Core SignalR during local development and Azure SignalR Service in production.

## Authentication and authorization

* Use ASP.NET Core Identity with cookie authentication.
* Supported roles are `User` and `Admin`.
* Apply authorization on the server, not only by hiding UI elements.
* Users can view resources, view schedules, and book available slots.
* Admins can additionally create, edit, and remove resources and view bookings across users.

## Code quality

* Use nullable reference types.
* Use asynchronous APIs for I/O operations.
* Pass `CancellationToken` through asynchronous application and persistence operations where appropriate.
* Prefer clear names over abbreviations.
* Keep controllers thin.
* Keep business logic outside Razor Views.
* Avoid unnecessary abstractions and speculative patterns.
* Do not introduce a repository abstraction merely to wrap every Entity Framework Core method.
* Add comments only when they explain a non-obvious decision or invariant.
* Treat compiler warnings as issues to investigate.

## Security

Never commit:

* passwords;
* connection strings containing credentials;
* API keys;
* Azure credentials;
* `.env` files;
* user secrets;
* generated build artifacts.

Use configuration providers, environment variables, and ASP.NET Core User Secrets for local secrets.

Do not log passwords, authentication cookies, access tokens, or complete connection strings.

## Change workflow

Before making a substantial change:

1. Inspect the relevant projects and dependencies.
2. Explain the intended design and affected files.
3. Keep the change focused.
4. Build the solution.
5. Run the relevant tests.
6. Summarize the result and any unresolved risks.

Do not create commits unless explicitly requested.

## Git conventions

Commits must be atomic and describe what changed and why.

Use concise imperative commit subjects, for example:

```text
Add room management domain model
Prevent concurrent booking conflicts
Broadcast booking updates through SignalR
```

Do not combine unrelated refactoring, formatting, and feature work in one commit.
