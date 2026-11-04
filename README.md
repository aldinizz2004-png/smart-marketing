# Smart Marketing Platform — Backend

An AI marketing platform for small shops: it learns the business, the owner sets a goal, the AI plans the
campaign and writes the content, the owner approves it, the platform publishes it inside the owner's limits,
then it measures the results.

**Stack:** ASP.NET Core Web API (.NET 10) · EF Core 10 · SQL Server · Docker / Docker Compose

```
SmartMarketingPlatform/            repository root
├── Dockerfile                     multi-stage build of the API image
├── .dockerignore                  keeps bin/obj, secrets and local data out of the build
├── compose.yaml                   API + SQL Server, volumes, network
├── .env.example                   template for the secrets (.env itself is never committed)
├── scripts/verify-docker.sh       checks the whole Docker setup (Week 5 checklist)
├── Database/SmartMarketingDb.sql  SQL script generated from the EF Core migrations
└── SmartMarketingPlatform/        the ASP.NET Core project
    ├── Entities/  Enums/          39 entities of the data model
    ├── Data/                      DbContext, Fluent API configurations, seed, migrator
    ├── Migrations/                EF Core migrations (InitialCreate, AddUserPasswordHash)
    ├── Controllers/  DTOs/  Services/  Common/  Workers/
    └── Resources/                 en.json / ar.json translations
```

---

## Run with Docker (Week 5 task)

### Prerequisites
- Docker Engine with the Compose plugin (`docker compose version`). On Windows: Docker Desktop, or Docker inside WSL 2.
- Nothing else: the .NET SDK and SQL Server are **not** needed on the host.

### 1. Configure the secrets
```bash
cp .env.example .env        # Windows PowerShell: Copy-Item .env.example .env
```
Edit `.env` and set your own values:

| Variable | Meaning |
|---|---|
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password (8+ chars, upper/lower case, digit, symbol; avoid `$` and `;`) |
| `JWT_KEY` | key that signs the access tokens (32+ random characters) |
| `API_HOST_PORT` / `DB_HOST_PORT` | host ports (default `8080` / `14333`) |
| `SWAGGER_ENABLED` | show Swagger UI (`true` / `false`) |

`.env` is listed in `.gitignore`. No password exists in the source code, the Dockerfile, compose.yaml or the image.

### 2. Build and start
```bash
docker compose up -d --build
```
- API: http://localhost:8080 — Swagger UI: http://localhost:8080/swagger — health: http://localhost:8080/health
- SQL Server (optional, for SSMS / sqlcmd): `localhost,14333`, user `sa`, password from `.env`

The first start takes about a minute: SQL Server initializes, then the API applies the migrations.

### 3. Useful commands
```bash
docker compose ps                    # status of both containers (db shows "healthy")
docker compose logs -f api           # API logs (migrations, publishing worker)
docker compose restart api           # restart only the API
docker compose down                  # stop and remove containers + network; DATA IS KEPT (volumes)
docker compose up -d                 # start again: same data
docker compose down -v               # stop AND delete the volumes (database + uploads): full reset
```

---

## How the Docker setup works

### Dockerfile (multi-stage)
1. **build** stage (`mcr.microsoft.com/dotnet/sdk:10.0`): copies the `.csproj` first and runs `dotnet restore`
   (this layer is cached and only re-runs when packages change), then copies the source and runs `dotnet publish -c Release`.
2. **final** stage (`mcr.microsoft.com/dotnet/aspnet:10.0`): only the ASP.NET runtime and the published output —
   no SDK and no source code. The app runs as the non-root `app` user and listens on port 8080.

### compose.yaml
| Service | Image | Role |
|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2025-latest` (Express) | the same engine as the EF Core assignment (SQL Server). A **healthcheck** runs `SELECT 1` with sqlcmd. |
| `api` | built from the Dockerfile | starts only when `db` is **healthy** (`depends_on: condition: service_healthy`). |

- **Network:** both services are on the `backend` network. The API reaches the database by its **service name**
  (`Server=db,1433` in the connection string): Docker's internal DNS resolves `db` to the database container.
- **Volumes (named):**
  - `sqldata` → `/var/opt/mssql`: the database files.
  - `appdata` → `/app/App_Data`: uploaded media, encrypted social-media tokens and the **Data Protection keys**
    that encrypt them. Without this volume a new container would generate new keys and could no longer read the saved tokens.
- **Configuration:** every setting comes from environment variables, using the ASP.NET Core convention
  `Section__Key` (for example `ConnectionStrings__MarketingDb`, `Jwt__Key`). Values marked `:?` in compose.yaml
  are required: compose refuses to start if they are missing from `.env`.

### EF Core migrations on startup
When `Database__ApplyMigrationsOnStartup=true` (set in compose.yaml), `Data/DatabaseMigrator.cs` runs before the API
and the background worker start:
1. lists the pending migrations and logs them,
2. calls `Database.MigrateAsync()`, which creates `SmartMarketingDb` if needed and applies every pending migration
   (the existing `InitialCreate` and `AddUserPasswordHash` — no new migration was created for Docker),
3. retries every 5 s (up to 10 times) while SQL Server is still starting.

On the next start the migrations are already in `__EFMigrationsHistory`, so nothing is applied twice and the data stays.
EF Core takes a database lock while migrating, so several API instances cannot migrate at the same time.
Outside Docker this setting is off; the database is updated with `dotnet ef database update` as before.

---

## Verify the setup (Week 5 checklist)

One command runs every check and prints PASS / FAIL:
```bash
bash scripts/verify-docker.sh
```

| # | Check | How it is verified |
|---|---|---|
| 1 | App image builds & starts | `docker compose up -d --build`, `/health` returns 200, container runs as non-root, no secret in the image |
| 2 | DB container running | `docker compose ps` → `db` is `healthy` |
| 3 | Migrations applied | API log "Applying 2 migration(s)", rows in `__EFMigrationsHistory` |
| 4 | Schema & FKs verified | 39 tables with primary keys, foreign keys present and none cascading (`DeleteBehavior.Restrict`), seed data (63 statuses, 9 platforms) |
| 5 | Data persists on restart | data is created through the API, then `docker compose down` + `up -d`: same users, same uploaded file (SHA-256), and an encrypted token is still readable (a post is published) |
| 6 | Network connectivity | the API container resolves `db`, `/health` reports the database as Healthy |

Manual checks:
```bash
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password from .env>" -C \
  -d SmartMarketingDb -Q "SELECT name FROM sys.tables ORDER BY name"
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password from .env>" -C \
  -d SmartMarketingDb -Q "SELECT OBJECT_NAME(parent_object_id) AS [Table], OBJECT_NAME(referenced_object_id) AS [References] FROM sys.foreign_keys ORDER BY 1"
```

---

## Run without Docker (development)
Requires the .NET 10 SDK, `dotnet-ef` and a local SQL Server (connection string in `appsettings.json`).
```bash
cd SmartMarketingPlatform
dotnet ef database update
dotnet run --launch-profile http        # http://localhost:5233/swagger
```

## Database
- Migrations: `SmartMarketingPlatform/Migrations/`
- SQL script generated from them: `Database/SmartMarketingDb.sql`
  (`dotnet ef migrations script -o ../Database/SmartMarketingDb.sql`)

> **Note on the EF Core assignment:** the entity design, relationships and the initial migration from the EF Core
> assignment are unchanged. The REST API and one additive migration (`AddUserPasswordHash`, a nullable column for
> login) were added later as part of the project. Docker applies all existing migrations automatically.
