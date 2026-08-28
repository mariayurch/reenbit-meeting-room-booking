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

## Security

Secrets, connection strings, Azure credentials, local environment files, and generated build artifacts must not be committed to the repository.
