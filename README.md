# Meeting Room Booking System

A web application for managing meeting rooms and booking fixed time slots with explicit concurrency control and real-time schedule updates.

The project was developed as a technical assignment for the Reenbit .NET Internship.

## Features

### Regular users

Authenticated users can:

- browse available meeting rooms;
- view a room schedule for a selected date;
- see which time slots are available or already booked;
- select and book one or more available time slots;
- receive a clear conflict response if another user books an overlapping slot first;
- see schedule changes in real time without refreshing the page.

### Administrators

Administrators can additionally:

- create meeting rooms;
- edit meeting rooms;
- activate or deactivate meeting rooms;
- create bookable time slots;
- access the administration area.

## Technology stack

- .NET 10
- ASP.NET Core MVC
- Razor Views
- ASP.NET Core Identity
- Cookie authentication
- Entity Framework Core
- SQL Server
- ASP.NET Core SignalR
- Azure SQL Database for production
- Azure SignalR Service for production
- xUnit
- Docker
- Azure App Service

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

### Project responsibilities

**MeetingRoomBooking.Domain**

Contains the core domain entities and business rules.

**MeetingRoomBooking.Application**

Contains application use cases and abstractions used by the application.

**MeetingRoomBooking.Infrastructure**

Contains Entity Framework Core persistence, SQL Server integration,
Identity persistence, and implementations of application abstractions.

**MeetingRoomBooking.Web**

Contains the ASP.NET Core MVC application, controllers, Razor Views,
authentication configuration, SignalR hub, and HTTP endpoints.

**MeetingRoomBooking.IntegrationTests**

Contains end-to-end integration tests covering authentication,
authorization, booking flow, persistence, and concurrent booking scenarios.

## Architecture

The solution follows a layered architecture:

```text
Web ────────────────> Application ─────> Domain
 │                         ▲
 └──────> Infrastructure ──┘
                 │
                 └─────────────────────> Domain
```

The Domain project has no dependency on persistence, ASP.NET Core,
Entity Framework Core, or presentation concerns.

## Prerequisites

Install:

- .NET 10 SDK
- Docker
- Git

Verify the installation:

```bash
dotnet --version
docker --version
git --version
```

## Local setup

### 1. Clone the repository

```bash
git clone https://github.com/mariayurch/reenbit-meeting-room-booking.git
cd reenbit-meeting-room-booking
```

### 2. Restore .NET tools and dependencies

```bash
dotnet tool restore
dotnet restore
```

### 3. Configure SQL Server

Copy the example environment file:

```bash
cp .env.example .env
```

Set a strong local SQL Server password in `.env`:

```env
MSSQL_SA_PASSWORD=your-strong-local-password
```

The `.env` file is excluded from version control and must not be committed.

Start SQL Server:

```bash
docker compose up -d
```

The application uses SQL Server 2022 running locally on port `1433`.

### 4. Configure the application connection string

The local connection string is stored with ASP.NET Core User Secrets.

Set it for the Web project:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=MeetingRoomBookingDb;User Id=sa;Password=your-strong-local-password;TrustServerCertificate=True" \
  --project src/MeetingRoomBooking.Web
```

Use the same password that was configured in `.env`.

### 5. Apply Entity Framework Core migrations

```bash
dotnet ef database update \
  --project src/MeetingRoomBooking.Infrastructure \
  --startup-project src/MeetingRoomBooking.Web
```

### 6. Optional: create a local administrator

The application automatically creates the `User` and `Admin` roles.

An administrator can optionally be seeded from configuration.

Store the local administrator credentials using User Secrets:

```bash
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" \
  --project src/MeetingRoomBooking.Web

dotnet user-secrets set "SeedAdmin:Password" "YourStrongAdminPassword123!" \
  --project src/MeetingRoomBooking.Web

dotnet user-secrets set "SeedAdmin:DisplayName" "Administrator" \
  --project src/MeetingRoomBooking.Web
```

All three values must be provided together.

If no admin seed configuration is supplied, the application starts normally
without creating an administrator account.

### 7. Run the application

```bash
dotnet run --project src/MeetingRoomBooking.Web
```

Open the local URL printed in the terminal.

Regular users can create their own account through the registration page.

## Booking concurrency design

Concurrency protection is explicit and database-backed.

A booking request is not implemented as an unprotected:

1. check whether a slot is free;
2. insert a booking.

That approach would allow two simultaneous requests to both observe the slot
as available before either booking is persisted.

Instead, each booking operation executes inside a SQL Server transaction.

### Room-level transactional locking

The booking operation reads the target meeting room using:

```sql
UPDLOCK, HOLDLOCK
```

inside a transaction using the `Read Committed` isolation level.

All booking requests for the same room therefore compete for the same
database lock.

When two requests arrive simultaneously:

1. one request acquires the room lock;
2. competing requests wait;
3. the first request validates the requested slots and creates the booking;
4. the transaction commits;
5. the next request acquires the lock;
6. it re-checks the slots against the committed database state;
7. if any requested slot is already booked, it receives a controlled conflict.

The losing request does not overwrite another booking and does not result in
an unhandled server error.

### Database uniqueness safeguard

A unique database index:

```text
UX_BookingTimeSlots_TimeSlotId
```

ensures that a `TimeSlot` cannot be associated with more than one booking.

This is a final database-level invariant in addition to the transactional
locking protocol.

SQL Server unique-constraint errors `2601` and `2627` are converted into a
booking conflict only when they correspond to this specific invariant.

Unrelated database errors are allowed to propagate rather than being
incorrectly presented as booking conflicts.

### Multi-slot bookings

Bookings containing multiple selected slots are atomic.

Either:

- all requested slots are booked successfully;

or:

- none of them are booked.

A partially completed multi-slot booking cannot remain in the database.

### Trade-off

The locking strategy operates at meeting-room level.

This intentionally favors a simple and deterministic concurrency protocol,
but it means bookings for different slots in the same room are serialized
while a booking transaction is in progress.

The lock is enforced by SQL Server rather than an in-process mutex, so the
same concurrency guarantee also works when multiple application instances
share the database.

## HTTP conflict behaviour

A successful booking redirects the user back to the room schedule.

If a concurrent request loses the race for one of the requested slots,
the application returns:

```text
HTTP 409 Conflict
```

with a clear explanation instead of returning a server error.

## Automated tests

Run all tests:

```bash
dotnet test
```

The integration test suite covers:

- booking access and role authorization;
- normal booking flow;
- simultaneous booking requests for the same slot;
- partially overlapping multi-slot booking requests.

### Concurrency test

The concurrency tests run against an isolated SQL Server container and use
real HTTP requests, authentication cookies, and antiforgery tokens.

Run only the concurrency tests:

```bash
dotnet test tests/MeetingRoomBooking.IntegrationTests \
  --filter FullyQualifiedName~BookingConcurrencyTests
```

The main concurrency scenario sends five simultaneous booking requests for
the same slot and verifies that:

- exactly one request succeeds;
- every losing request receives HTTP `409 Conflict`;
- exactly one booking is persisted;
- the persisted booking belongs to the winning user;
- only the winner's slot associations remain in the database.

A second scenario verifies concurrent requests containing partially
overlapping slot ranges.

Docker must be running for the SQL Server integration tests.

## Real-time schedule updates

The application uses ASP.NET Core SignalR.

Clients viewing a room schedule connect to:

```text
/hubs/room-schedule
```

When booking status changes, connected viewers receive the update immediately
without refreshing the page.

ASP.NET Core SignalR is used during local development.

Azure SignalR Service is intended for the deployed environment.

## Security and configuration

Sensitive configuration is not stored in source control.

Do not commit:

- `.env`;
- database passwords;
- connection strings containing credentials;
- administrator passwords;
- Azure credentials;
- API keys.

Local application secrets are stored with ASP.NET Core User Secrets.

Environment variables and Azure configuration will be used for deployed
infrastructure.

## Code quality

The project uses:

- nullable reference types;
- asynchronous APIs for I/O;
- server-side authorization;
- explicit application abstractions;
- database migrations;
- integration tests for infrastructure-sensitive behavior;
- atomic Git commits.

Concurrency behaviour is intentionally documented because it is a core
architectural requirement of the assignment.

## Development process and Claude Code

The repository contains [`CLAUDE.md`](CLAUDE.md), which documents the
project architecture, development rules, concurrency requirements, testing
strategy, security constraints, and instructions used during AI-assisted
development.

## Azure deployment

The production environment is designed to use:

- Azure App Service;
- Azure SQL Database;
- Azure SignalR Service.

Deployment configuration and the public application URL will be documented
here after the Azure infrastructure is created.