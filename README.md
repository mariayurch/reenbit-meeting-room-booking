# Meeting Room Booking System

A web application for managing meeting rooms and booking fixed time slots with explicit concurrency control and real-time schedule updates.

The project is being developed as a technical assignment for the Reenbit .NET Internship.

## Status

Initial solution structure. Application features are under active development.

## Core requirements

* User registration and cookie-based authentication
* `User` and `Admin` roles
* Meeting room management for administrators
* Fixed bookable time slots
* Conflict-safe concurrent booking
* Clear conflict responses for unsuccessful simultaneous bookings
* Automated concurrency testing
* Real-time schedule updates
* Azure deployment

## Technology stack

* .NET 10
* ASP.NET Core MVC with Razor Views
* ASP.NET Core Identity with cookie authentication
* Entity Framework Core
* SQL Server for local development
* Azure SQL Database for production
* ASP.NET Core SignalR
* Azure SignalR Service
* xUnit
* Docker
* Azure App Service

## Solution structure

```text
src/
├── MeetingRoomBooking.Domain
├── MeetingRoomBooking.Application
├── MeetingRoomBooking.Infrastructure
└── MeetingRoomBooking.Web

tests/
├── MeetingRoomBooking.UnitTests
└── MeetingRoomBooking.IntegrationTests
```

### Projects

* **Domain** contains core entities and business rules.
* **Application** contains application use cases and abstractions.
* **Infrastructure** contains persistence and external-service implementations.
* **Web** contains the ASP.NET Core MVC application, Razor Views, authentication configuration, and HTTP endpoints.
* **UnitTests** verifies isolated business rules.
* **IntegrationTests** verifies interactions between the web application, persistence layer, and database, including concurrent booking scenarios.

## Architecture dependencies

```text
Web ────────────────> Application ─────> Domain
 │                         ▲
 └──────> Infrastructure ──┘
                 │
                 └─────────────────────> Domain
```

The Domain project has no dependencies on infrastructure or presentation concerns.

## Prerequisites

* .NET 10 SDK
* Docker
* Git

## Build

```bash
dotnet restore
dotnet build
```

## Run tests

```bash
dotnet test
```

## Run the web application

```bash
dotnet run --project src/MeetingRoomBooking.Web
```

The application prints its local URL in the terminal after startup.

## Development process

Development follows small, atomic commits. Significant architectural and concurrency decisions will be documented as the implementation evolves.

Claude instructions are maintained in [`CLAUDE.md`](CLAUDE.md).

## Booking concurrency control

Booking uses an explicit SQL Server transaction with room-level locking
and a unique database index as a final safeguard.

### Transaction and locking

Each booking request starts a transaction at the Read Committed
isolation level and reads the meeting room using `UPDLOCK, HOLDLOCK`.

Competing booking requests for the same room must acquire the same
lock, so they wait until the current transaction completes.
All booking writers must follow this locking protocol.

While holding the lock, the application validates the selected slots
and checks for existing confirmed bookings. It then saves the booking
and all slot associations in the same transaction.

If a selected slot is already booked, the request returns a conflict
without creating a booking.

### Database safeguard

The unique index `UX_BookingTimeSlots_TimeSlotId` on
`BookingTimeSlots.TimeSlotId` prevents a slot from being associated
with more than one booking.

SQL Server uniqueness errors 2601 and 2627 are converted into a
conflict only when they identify this specific index. The transaction
is rolled back and failed tracked changes are cleared.
Unrelated database errors are not disguised as booking conflicts.

### HTTP behaviour

A successful booking redirects the user to the room schedule.

A conflicting booking returns HTTP `409 Conflict` and displays
an explanation on the schedule page.

Multi-slot bookings are atomic: either all requested slots are
booked or none are.

### Trade-offs

The room lock keeps the concurrency protocol simple, but also
serializes bookings for different slots within the same room.

The lock is enforced by SQL Server rather than an in-process mutex,
so the protocol also applies when multiple application instances
use the same database.

### Automated verification

Integration tests use an isolated SQL Server container, real
authentication cookies, antiforgery tokens, and concurrent HTTP requests.

The tests cover:

- Five users attempting to book the same slot.
- Two users attempting to book partially overlapping two-slot ranges.

Each scenario verifies exactly one successful redirect, HTTP `409`
for every losing request, one persisted booking belonging to the
winner, and only the winner's slot associations remaining in the database.

Run these tests with Docker running:

    dotnet test tests/MeetingRoomBooking.IntegrationTests --filter FullyQualifiedName~BookingConcurrencyTests

## Security

Secrets, connection strings, Azure credentials, local environment files, and generated build artifacts must not be committed to the repository.
