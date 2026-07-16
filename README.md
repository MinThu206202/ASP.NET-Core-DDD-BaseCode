# UserApp — ASP.NET Core 8 Enterprise Starter

A production-grade Clean Architecture starter with RBAC, audit logging, real-time notifications (SignalR), Redis caching, background jobs (Quartz), email (MailKit), and Docker Compose.

---

## Architecture

### Clean Architecture Layers

```
┌──────────────────────────────────────────────────────────────┐
│                     UserApp.Web (MVC + API)                  │
│  Controllers · Views · ViewModels · Filters · Middleware     │
│  Depends on: Application, Infrastructure                     │
├──────────────────────────────────────────────────────────────┤
│              UserApp.Application (Use Cases)                 │
│  Services · DTOs · Interfaces · AutoMapper Profiles          │
│  Depends on: Domain only                                     │
├──────────────────────────────────────────────────────────────┤
│            UserApp.Infrastructure (Persistence)              │
│  EF Core · Repositories · BCrypt · MailKit · SignalR Hubs    │
│  Quartz Jobs · Redis · Media Pipeline                        │
│  Depends on: Domain, Application (interfaces only)           │
├──────────────────────────────────────────────────────────────┤
│          UserApp.Domain (Enterprise Core)                    │
│  Entities · Value Objects · Enums · Repository Interfaces    │
│  Dependencies: Zero                                          │
└──────────────────────────────────────────────────────────────┘
```

### Deployment Topology

```
                    ┌────────────┐
                    │   nginx    │  (port 5002 → 80)
                    │  reverse   │
                    │  proxy     │
                    └─────┬──────┘
                          │
                    ┌─────▼──────┐
                    │  Web App   │  (dotnet watch, hot-reload)
                    │  :80       │
                    └──┬──┬──┬───┘
                       │  │  │
              ┌────────┘  │  └──────────┐
              │           │             │
         ┌────▼───┐  ┌────▼───┐   ┌────▼────┐
         │ MySQL  │  │ Redis  │   │ Quartz  │
         │  8.0   │  │   7    │   │ Scheduler│
         └────────┘  └────────┘   └─────────┘
```

---

## Request Lifecycle (Project Flow)

```
HTTP Request
    │
    ▼
nginx (reverse proxy)
    │  ── Proxy headers (X-Real-IP, X-Forwarded-*, Upgrade/WebSocket)
    │  ── Security headers (CSP, X-Frame-Options, etc.)
    ▼
ASP.NET Core Pipeline
    │
    ├── ExceptionHandler (global error handling)
    ├── RateLimiter (per-IP, token bucket)
    ├── AuthenticationMiddleware (Cookie for MVC, JWT for API)
    ├── AuthorizationMiddleware
    │
    ├── PermissionFilter (IAsyncActionFilter)
    │   └── Checks that current user has required permission for the action
    │       └── If denied → redirects to /Auth/Denied
    │
    ├── MiniProfiler (dev only)
    │
    └── Controller Action
        │
        ├── Validates ViewModel (FluentValidation)
        ├── Calls Application Service (e.g. IAuthService)
        │       │
        │       └── Application Service
        │               ├── Loads/validates Domain entity
        │               ├── Calls Repository (Infrastructure)
        │               └── Returns DTO / Result
        │
        ├── (Optional) Fires INotificationService.SendAsync()
        │       │
        │       ├── Creates Notification entity → saves to DB
        │       ├── Calls INotificationDispatcher.DispatchAsync()
        │       │       └── SignalRNotificationChannel
        │       │           └── Pushes to user:{RecipientId} group
        │       └── Marks as Delivered → saves again
        │
        ├── (Optional) Enqueues email via IEmailTaskQueue
        │       └── EmailBackgroundService processes in background
        │
        └── Returns View / Redirect
```

---

## Feature Deep-Dive

### 1. RBAC (Role-Based Access Control)

```
Role ───────< RolePermission >─────── Permission
  │                                       │
  │                                  (auto-synced from
  │                                   Controller/Action
  │                                   attributes)
  │
UserRole (many-to-many join)
  │
User
```

- Every action method has a `[Permission("Module", "Action")]` attribute
- `PermissionFilter` intercepts every request and calls `IPermissionChecker.HasPermissionAsync()`
- Permissions are auto-synced via the **Sync** button in Role Permissions UI
- Super Admin role bypasses all checks

### 2. Audit Logging

- Every Create/Edit/Delete on entities with `[Auditable]` attribute auto-generates audit logs
- Captures: old values → new values diff, who made the change, IP address, timestamp
- Supports **restore** and **revert** via the audit log UI
- `AuditLogArchiveJob` (Quartz, daily at 09:00 Asia/Yangon) archives logs older than 30 days

### 3. Notification System (Real-Time + Persisted)

```
Controller
  │  INotificationService.SendAsync(request)
  ▼
NotificationService
  │
  ├── 1. new Notification() → MarkAsQueued()
  ├── 2. INotificationRepository.AddAsync()
  ├── 3. IUnitOfWork.SaveChangesAsync()
  ├── 4. INotificationDispatcher.DispatchAsync()
  │       │
  │       └── SignalRNotificationChannel.SendAsync()
  │           └── IHubContext<NotificationHub>
  │               └── Clients.Group("user:{RecipientId}")
  │                   └── "ReceiveNotification" event
  │
  ├── 5. MarkAsDelivered()
  └── 6. IUnitOfWork.SaveChangesAsync()
```

**SignalR Connection Flow:**

```
1. User logs in → cookie issued with ClaimTypes.NameIdentifier = user GUID
2. Layout.cshtml creates SignalR connection to /hubs/notifications
3. NotificationHub.OnConnectedAsync() runs:
   - Context.UserIdentifier = GUID (from auth cookie, automatically mapped)
   - Groups.AddToGroupAsync(connectionId, "user:{GUID}")
4. Any notification sent to that RecipientId:
   - _hubContext.Clients.Group("user:{GUID}").SendAsync("ReceiveNotification", data)
5. Client receives event:
   - Increments bell badge number (navbar)
   - Shows toast notification (auto-dismiss after animation)
```

**Notification Types:**

| Type | Example Trigger |
|------|----------------|
| `UserCreated` | Registration, admin creates user |
| `UserUpdated` | Admin edits user |
| `UserDeleted` | Admin deletes user |
| `RoleAssigned` / `RoleRemoved` | Admin changes user roles |
| `PermissionGranted` / `PermissionRevoked` | Admin edits role permissions |
| `LoginDetected` | User logs in |
| `PasswordChanged` | User resets password |
| `SystemAnnouncement` | New user registered (to all admins) |

### 4. Email (Background Queue)

```
Controller (fire-and-forget)
  │  _emailTaskQueue.EnqueueAsync(task)
  ▼
IEmailTaskQueue (Channel<Func<IServiceProvider, CancellationToken, Task>>)
  │
  ▼
EmailBackgroundService (BackgroundService, singleton)
  │  dequeues → creates DI scope → executes
  ▼
IEmailService (MailKit SMTP)
  │  SendAsync() or SendTemplateAsync()
  └── Supports Markdown templates (email-to-HTML via Markdig)
```

- Registration welcome email is **always queued** — response is never blocked by SMTP latency
- Admin notification emails are also queued
- Queue capacity: 100, drops oldest on overflow (configurable)

### 5. Redis Caching (Cache-Aside Pattern)

Applied in `BaseService<T>`:

```
GetByIdAsync(id):
  1. Try _cacheService.GetAsync<T>(cacheKey)
  2. If hit → return
  3. If miss → load from DB → store in cache (30-min TTL) → return

UpdateAsync(entity):
  1. Update in DB
  2. Remove cache key (invalidate, not update)
```

### 6. Auth (Dual Auth)

| Mechanism | Where | Expiry |
|-----------|-------|--------|
| Cookie (MVC) | `[Authorize]` on controllers | 2h sliding |
| JWT Bearer (API) | `/api/*` endpoints | 1h access + 7d refresh (rotated) |

- Forgot password flow: email → OTP (5-min TTL, 5 attempts, 1h block) → password reset
- Refresh token rotation invalidates old tokens on use

### 7. Scheduled Jobs (Quartz)

| Job | Schedule | What it does |
|-----|----------|-------------|
| `AuditLogArchiveJob` | Daily 09:00 Asia/Yangon | Archives audit logs older than 30 days |

---

## Quick Start

### Prerequisites

- [Docker Desktop](https://docs.docker.com/desktop/) (with Compose v2)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional, for CLI commands)

### 1. Clone and configure

```bash
git clone <repo-url> && cd Basecode-ASP.NET-Core
```

Edit `.env` if needed (defaults work out of the box):

```bash
MYSQL_PORT=3308
REDIS_PORT=6380
NGINX_PORT=5002
```

For emails, edit `src/UserApp.Web/appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "YOUR_EMAIL@gmail.com",
  "Password": "YOUR_GMAIL_APP_PASSWORD",
  "FromEmail": "YOUR_EMAIL@gmail.com",
  "FromName": "UserApp"
}
```

### 2. Start everything

```bash
docker compose up -d
```

This starts 4 containers:

| Container | Host Port | Purpose |
|-----------|-----------|---------|
| `DotNet-Core-Basecode-web` | internal | ASP.NET Core 8 (hot-reload) |
| `DotNet-Core-Basecode-nginx` | 5002 | Reverse proxy |
| `DotNet-Core-Basecode-mysql` | 3308 | Database |
| `DotNet-Core-Basecode-redis` | 6380 | Cache |

### 3. Open the app

http://localhost:5002

### 4. Log in

| Email | Password | Role |
|-------|----------|------|
| `admin@local.com` | `Admin123!` | Admin (full access) |
| `user@local.com` | `User123!` | User (limited) |

### 5. Sync permissions (first time only)

Go to **Role Permissions** in the sidebar → click **Sync**. This scans all controllers/actions and seeds the permission table.

---

## Development

### Hot reload

Source files are mounted into the container. `dotnet watch` detects changes and recompiles automatically:

```bash
docker compose logs web -f    # watch rebuild
```

### EF Core migrations

```bash
# Create
docker compose exec web dotnet ef migrations add <Name> \
  --project src/UserApp.Infrastructure

# Apply (runs auto on startup, but manual works too)
docker compose exec web dotnet ef database update \
  --project src/UserApp.Infrastructure
```

### Useful commands

```bash
docker compose ps             # health status
docker compose logs web -f    # follow logs
docker compose down           # stop (volumes persist)
docker compose down -v        # stop + fresh DB
```

### Run without Docker

```bash
# Start MySQL and Redis manually, then:
dotnet run --project src/UserApp.Web
```

---

## Project Structure

```
src/
├── UserApp.Domain/                # Zero dependencies
│   ├── AuditLogs/                 # AuditLog entity + interfaces
│   ├── Common/                    # Base entity, value objects, repository interfaces
│   ├── CommonTables/              # Lookup/dictionary entities
│   ├── Media/                     # Media/pipeline entities
│   ├── Notifications/             # Notification entity, enums, repository interface
│   ├── Permission/                # Permission entity + interfaces
│   ├── Roles/                     # Role, UserRole entities + interfaces
│   └── Users/                     # User entity, Email value object, interfaces
│
├── UserApp.Application/           # Depends on Domain only
│   ├── AuditLogs/
│   ├── Common/                    # BaseService, cache-aside pattern
│   ├── CommonTables/
│   ├── Common/Interfaces/         # IEmailService, IEmailTaskQueue, ICacheService, etc.
│   ├── Media/
│   ├── Notifications/
│   │   ├── DTOs/                  # CreateNotificationRequest, NotificationDto
│   │   ├── Interfaces/            # INotificationDispatcher
│   │   └── Services/              # INotificationService, NotificationService
│   ├── Permissions/
│   ├── Roles/
│   └── Users/
│
├── UserApp.Infrastructure/        # Depends on Domain + Application (interfaces)
│   ├── Background/                # EmailTaskQueue, EmailBackgroundService
│   ├── EmailTemplates/            # Markdown email templates
│   ├── Identity/                  # BCrypt password hasher
│   ├── Media/
│   ├── Migrations/                # EF Core migrations
│   ├── Notifications/
│   │   ├── Channels/              # INotificationChannel, SignalRNotificationChannel
│   │   ├── Dispatchers/           # NotificationDispatcher
│   │   ├── Hubs/                  # NotificationHub (SignalR)
│   │   └── Repositories/         # NotificationRepository
│   ├── Persistence/              # AppDbContext, UnitOfWork, seeders
│   ├── Security/                  # PermissionChecker
│   └── Services/                  # EmailService (MailKit)
│
├── UserApp.Web/                   # Entry point
│   ├── Controllers/
│   │   ├── Api/                   # JWT-protected API endpoints
│   │   ├── AuthController.cs      # Login, Register, ForgotPassword, OTP, ChangePassword
│   │   ├── NotificationController.cs
│   │   ├── RolesController.cs
│   │   ├── UsersController.cs
│   │   └── ...
│   ├── Views/
│   │   └── Notification/Index.cshtml
│   ├── ViewModels/
│   ├── Common/                    # PermissionFilter, DynamicValidator
│   ├── Jobs/                      # AuditLogArchiveJob (Quartz)
│   └── Program.cs                 # DI, middleware pipeline
│
├── nginx/
│   └── nginx.conf                 # Reverse proxy + security headers
│
├── docker-compose.yml
├── .env                           # Secrets (gitignored in production)
└── README.md
```

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "MySql": "server=127.0.0.1;port=3308;database=userapp;user=userapp;password=...",
    "Redis": "127.0.0.1:6380"
  },
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "UserApp",
    "Audience": "UserAppClient"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "YOUR_EMAIL@gmail.com",
    "Password": "YOUR_GMAIL_APP_PASSWORD",
    "FromEmail": "YOUR_EMAIL@gmail.com",
    "FromName": "UserApp"
  }
}
```

Docker Compose overrides connection strings via environment variables (`ConnectionStrings__MySql`, `ConnectionStrings__Redis`).

---

## API

API endpoints live under `/api/` with JWT Bearer auth. Available controllers in `Controllers/Api/`:

| Controller | Endpoints |
|-----------|-----------|
| `UsersApi` | CRUD users |
| `RolesApi` | CRUD roles |
| `PermissionsApi` | List/sync permissions |
| `AuthApi` | Login, refresh token |
| `AuditLogApi` | Query audit logs |
| `CommonTableApi` | Lookup data CRUD |

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 |
| ORM | EF Core 8 + Pomelo MySQL |
| Auth (MVC) | Cookie (2h sliding) |
| Auth (API) | JWT Bearer (1h access, 7d refresh, rotated) |
| Password | BCrypt (BCrypt.Net-Next) |
| Caching | StackExchange.Redis (cache-aside, 30-min TTL) |
| Real-time | SignalR (auto-reconnect, user groups) |
| Background | Quartz.NET + Channel-based email queue |
| Email | MailKit + Markdig (Markdown templates) |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Proxy | nginx (alpine) |
| DB | MySQL 8.0 |
| Cache Store | Redis 7 Alpine |
| UI | Tailwind CSS (CDN) + Bootstrap 5 |
