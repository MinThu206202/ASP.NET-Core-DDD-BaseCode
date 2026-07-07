# UserApp — Enterprise ASP.NET Core 8 Starter

A full-featured enterprise starter with **RBAC**, **audit logging**, **nginx reverse proxy**, and **Docker Compose** — all running with hot-reload for development.

## Architecture

```
                   ┌──────────┐
                   │  nginx   │  (reverse proxy, port 5001)
                   └────┬─────┘
                        │
                   ┌────▼─────┐
                   │  Web     │  (ASP.NET Core 8 MVC)
                   └────┬─────┘
                        │
              ┌─────────┼─────────┐
              │         │         │
         ┌────▼───┐ ┌──▼────┐ ┌──▼────┐
         │ MySQL  │ │ Redis │ │ Quartz│
         │  (8.0) │ │ (7)   │ │ Jobs  │
         └────────┘ └───────┘ └───────┘
```

**Layers** (dependency flows inward):
- `UserApp.Domain` — Entities, value objects, repository interfaces (zero dependencies)
- `UserApp.Application` — Use cases, DTOs, service abstractions
- `UserApp.Infrastructure` — EF Core, repositories, BCrypt, Quartz jobs
- `UserApp.Web` — MVC controllers, views, API controllers

## Features

| Feature | Details |
|---------|---------|
| **RBAC** | Roles, Permissions, Role-Permission mapping, PermissionFilter on every action |
| **Audit Log** | Auto-logs all Create/Edit/Delete actions with old/new values & restore/revert |
| **Common Tables** | Lookup/dictionary data management |
| **Auth** | Cookie auth + JWT for API, forgot password, OTP verification |
| **Soft Delete** | All entities support soft delete with restore |
| **File Upload** | Per-entity media attachments (JPG/PNG/WEBP, 5MB limit) |
| **Health Checks** | Every service has Docker health checks |
| **Hot Reload** | `dotnet watch` — edit code, auto-restart |
| **Reverse Proxy** | nginx handles incoming requests with proper headers |
| **Redis** | Caching layer ready (StackExchange.Redis configured) |
| **Quartz** | Scheduled jobs (audit log archiving) |

## Quick Start

### Prerequisites

- [Docker Desktop](https://docs.docker.com/desktop/) (with Compose v2)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional, for CLI commands)

### 1. Start everything

```bash
git clone <repo-url> && cd Basecode-ASP.NET-Core
docker compose up -d
```

Docker Compose starts 4 services:

| Service | Port | Credentials |
|---------|------|-------------|
| nginx | `5001` | — |
| web | internal | — |
| mysql | `3307` | see `.env` |
| redis | `6379` | — |

### 2. Open the app

http://localhost:5001

### 3. Log in

| Email | Password | Role |
|-------|----------|------|
| `admin@local.com` | `Admin123!` | Admin (full access) |
| `user@local.com` | `User123!` | User (limited) |

### 4. (First time only) Sync permissions

After startup, go to **Role Permissions** in the sidebar and click **Sync** to scan all controllers and seed permissions into the database.

## Configuration

### Environment variables (`.env`)

```bash
COMPOSE_PROJECT_NAME=dotnet-core-basecode

# MySQL
MYSQL_ROOT_PASSWORD=rootpw
MYSQL_DATABASE=userapp
MYSQL_USER=userapp
MYSQL_PASSWORD=userapp_pw
```

> **Security:** Add `.env` to `.gitignore` in production. For local development the defaults work out of the box.

### appsettings

Override any setting via `appsettings.Development.json` or environment variables. Connection strings use the `ConnectionStrings__MySql` convention so Docker Compose injects them automatically.

## Development

### Hot reload

The `web` service runs `dotnet watch run` with source mounted from `./src`. Any file change triggers an automatic rebuild and restart:

```bash
docker compose up -d          # start with hot reload
docker compose logs web -f    # watch for recompilation
```

### Run without Docker

```bash
# Start MySQL and Redis manually, then:
dotnet run --project src/UserApp.Web
```

### EF Core migrations

```bash
# Create a migration
docker compose exec web dotnet ef migrations add <Name> \
  --project src/UserApp.Infrastructure

# Apply (runs automatically on startup, but manual works too)
docker compose exec web dotnet ef database update \
  --project src/UserApp.Infrastructure
```

### Useful commands

```bash
docker compose ps             # check health status
docker compose logs web -f    # follow web logs
docker compose down           # stop (data persists)
docker compose down -v        # stop + delete volumes (fresh DB)
```

## Project Structure

```
src/
  UserApp.Domain/           Entities, value objects, repository interfaces
  UserApp.Application/      Services, DTOs, interfaces
  UserApp.Infrastructure/   EF Core DbContext, repositories, BCrypt, Quartz, seeding
  UserApp.Web/
    Controllers/            MVC + API controllers
    Views/                  Razor views (Tailwind CSS)
    ViewModels/             View models per module
    Common/                 PermissionFilter, DynamicValidator
    wwwroot/uploads/        File uploads
nginx/
  nginx.conf                Reverse proxy config
docker-compose.yml          Full stack (dev mode, no build needed)
.env                        Secrets
```

## API

API endpoints live under `/api/` with JWT authentication. See the `Controllers/Api/` folder for available endpoints (`UsersApi`, `RolesApi`, `PermissionsApi`, `AuthApi`, `AuditLogApi`, `CommonTableApi`).

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 |
| ORM | EF Core 8 + Pomelo MySQL |
| Auth | Cookie + JWT Bearer |
| Password | BCrypt (BCrypt.Net-Next) |
| Caching | StackExchange.Redis |
| Scheduler | Quartz.NET |
| Mapping | AutoMapper |
| Proxy | nginx (alpine) |
| DB | MySQL 8.0 |
| Cache | Redis 7 Alpine |
| UI | Tailwind CSS, Bootstrap 5 |
