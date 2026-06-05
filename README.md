# UserApp — Enterprise DDD (ASP.NET Core 8 Razor Pages + MySQL)

A clean, layered DDD starter for a **User module**: Razor Pages UI, EF Core
with Pomelo MySQL provider, BCrypt password hashing, and a Docker Compose
MySQL service you can browse with **MySQL Workbench**.

## Solution layout

```
UserApp.sln
src/
  UserApp.Domain         # Entities, Value Objects, Repository interfaces
  UserApp.Application    # Use cases (UserService), DTOs, abstractions
  UserApp.Infrastructure # EF Core DbContext, repositories, BCrypt hasher, DI
  UserApp.Web            # ASP.NET Core Razor Pages (presentation)
docker-compose.yml       # MySQL 8 for local dev
```

Dependency direction: `Web → Infrastructure → Application → Domain`.
Domain has zero outward dependencies.

## Prerequisites (macOS)

1. **.NET 8 SDK**
   ```bash
   brew install --cask dotnet-sdk
   dotnet --version   # expect 8.x
   ```
2. **Docker Desktop for Mac** (Apple Silicon or Intel)
   ```bash
   brew install --cask docker
   open -a Docker
   ```
3. **MySQL Workbench**
   ```bash
   brew install --cask mysqlworkbench
   ```
4. **EF Core CLI tool** (one-time, global)
   ```bash
   dotnet tool install --global dotnet-ef
   ```

## 1) Start MySQL with Docker

From the project root:

```bash
docker compose up -d
docker compose ps     # mysql should be "healthy"
```

Connection info (matches `appsettings.json`):

| Field    | Value         |
|----------|---------------|
| Host     | `127.0.0.1`   |
| Port     | `3306`        |
| Database | `userapp`     |
| User     | `userapp`     |
| Password | `userapp_pw`  |
| Root pwd | `rootpw`      |

### Connect with MySQL Workbench
- Open Workbench → **+** next to "MySQL Connections"
- Connection Name: `UserApp Local`
- Hostname: `127.0.0.1`, Port: `3306`
- Username: `userapp` → click **Store in Keychain** → enter `userapp_pw`
- **Test Connection** → **OK**

## 2) Restore, build, migrate

```bash
dotnet restore
dotnet build

# Create initial migration (only the first time)
dotnet ef migrations add Initial \
  --project src/UserApp.Infrastructure \
  --startup-project src/UserApp.Web

# (Migrations also run automatically on app startup, but you can apply manually.)
dotnet ef database update \
  --project src/UserApp.Infrastructure \
  --startup-project src/UserApp.Web
```

## 3) Run the web app

```bash
dotnet run --project src/UserApp.Web
```

Open http://localhost:5080 — you'll see the home page. Click **Manage Users**
to list, create, edit, and delete users.

## Domain model highlights

- `User` aggregate with private setters and factory `User.Create(...)`
- `Email` value object (record) with validation
- `UserStatus` enum, `IUserRepository` repository abstraction in Domain
- EF Core configuration via `IEntityTypeConfiguration<User>` (snake_case columns)
- BCrypt password hashing (`BCrypt.Net-Next`)

## Useful commands

```bash
# Stop / wipe MySQL
docker compose down            # keep data
docker compose down -v         # delete volume (fresh DB)

# Add another migration after a domain change
dotnet ef migrations add <Name> \
  --project src/UserApp.Infrastructure \
  --startup-project src/UserApp.Web
```

## Next steps

- Add ASP.NET Core Identity or JWT auth on top of `User`
- Domain events + MediatR for CQRS
- FluentValidation for command validation
- Unit tests project for `UserApp.Domain` and `UserApp.Application`
