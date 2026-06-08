# SLT.Manage — Admin / Back-Office API

`SLT.Manage` is the **admin/back-office management API** for the SLT crypto cargo-payment
platform. It is a read-only **reporting and admin-login backend** over a shared MongoDB:
admins authenticate by username/password, then query aggregate/wallet/order/invoice reports.
It is the sibling of the public-facing `slt.api` and reuses the same `SLT.Utilities`
cross-cutting framework and the same Mongo collections (it reads what `slt.api` writes).

This service performs **no on-chain work** — no RPC calls, no signing, no transaction
verification. "Blockchain" data (token symbols, payment hashes, wallets, block numbers)
exists only as persisted MongoDB records that the reports read and aggregate.

## Architecture (N-tier, layered)

Classic four-project layered Web API — **not** Clean Architecture / DDD / CQRS / EF.
One-way dependency chain:

```
SLT.Manage   (Microsoft.NET.Sdk.Web — host/API, controllers, pipeline)  ──► SLT.Services
SLT.Services (class lib — _<Module> feature services + DTOs)            ──► SLT.Domain
SLT.Domain   (class lib — Mongo collections + repositories)             ──► SLT.Utilities
SLT.Utilities(class lib — all cross-cutting framework infra)            ──► (no project refs)
```

"Domain" here means **Mongo entities + repositories**, not DDD aggregates. See
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the request lifecycle, middleware pipeline,
the auth layers, and an honest architecture assessment.

## Tech stack (verified from `.csproj`)

- **.NET 8** (`net8.0`, C# 12 — no `LangVersion` override). `<Nullable>disable</Nullable>` and
  `<ImplicitUsings>enable</ImplicitUsings>` on all projects.
- **MongoDB** via the custom **"Monjo"** wrapper (`MonjoRepository<T>`, `IMonjoConnection`,
  `MonjoQuery`) over `MongoDB.Driver` 2.28.0. **No EF Core, no SQL, no `DbContext`.**
- **Autofac** DI by **marker-interface convention scanning** (no per-type registration).
- **JWE tokens** — JWT that is signed (HMAC-SHA256) *and* encrypted (AES-128).
- Sentry (error capture), BCrypt.Net (password hashing), `Asp.Versioning.Mvc`, Swashbuckle.
- **No formatter enforced, no test project** — but CI runs on GitLab (Release build + advisory csharpier check + centralized package versions + Roslyn analyzer warnings). See [docs/CI.md](docs/CI.md).

## Service modules

| Module | Folder | Responsibility |
|---|---|---|
| `_User` | `SLT.Services/_User/` | Admin login (client-id/secret + BCrypt password verify → JWE), JWT blacklist storage |
| `_Report` | `SLT.Services/_Report/` | 8 read endpoints: hash/order/wallet lookups + system totals |
| `_Log` | `SLT.Services/_Log/` | Request/event logging + hard-delete retention (`RealDeleteManyAsync`) |

Controllers (`SLT.Manage/Controllers/V1/`, all `POST`-only, `api/v1/[controller]`):

- **`AuthController`** — `POST api/v1/Auth/LoginAsync` (admin login → JWE).
- **`ReportController`** — 8 read endpoints, all `[Authorize(Permissions.Reporter)]`:
  `GetSingleHashInvoiceData`, `GetOrderDetail`, `GetSingleWalletOverview`,
  `GetSingleWalletOrders`, `GetSingleWalletInvoices`, `GetTotalOverview`, `GetAllOrders`,
  `GetAllInvoices`.

Mongo collections (in `SLT.Domain/Collections/`): `User`, `Order`, `Invoice`,
`TransactionLog`, `Log`, `RequestLog`.

## Build / run / deploy

```bash
# Build
dotnet build SLT.Manage/SLT.Manage.csproj -c Release      # or: dotnet build SLT.Manage.slnx

# Run (dev) — host is Microsoft.NET.Sdk.Web; Swagger UI is served in all environments
dotnet run --project SLT.Manage

# Publish + containerize + restart (the real flow)
./deploy.sh
#   dotnet publish ... -o publish
#   docker build -t gate.api .            (image name shared with a gateway, not Manage-specific)
#   docker-compose down && docker-compose up -d
```

- **Dockerfile:** `FROM mcr.microsoft.com/dotnet/aspnet:8.0`, entrypoint `dotnet SLT.Manage.dll`,
  exposes 80/443.
- **docker-compose.yml:** container `gate.sltcargopay.com`, host-mapped `127.0.0.1:3008:80`.
  Runtime config/secrets injected as `__`-delimited env vars
  (`MonjoSettings__ConnectionString`, `JwtServiceSettings__*`, `ApplicationPoolSettings__*`)
  sourced from shell `$variables`.
- CI runs a Release **build** + an advisory **csharpier** format check on every MR; **deploy** is a manual-gated job on the self-hosted `sltmanage-prod` runner. There is still **no test step**. See [docs/CI.md](docs/CI.md).

## Security

`SLT.Manage/appsettings.json` ships **live-looking secrets in source control** (Mongo
connection string with credentials, JWT signature/encryption keys, client secrets, per-app
pre-shared keys + master signatures). Treat as a leaked-secret finding. See
[docs/SECURITY.md](docs/SECURITY.md) for the full findings and remediation.

## More docs

- [CLAUDE.md](CLAUDE.md) — AI entry point (gotchas, dependency chain, namespaces).
- [CODEBASE_MAP.md](CODEBASE_MAP.md) — where code lives + "to add X, edit Y".
- [CONTRIBUTING.md](CONTRIBUTING.md) — commit messages, MR workflow, local validation.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) · [docs/SECURITY.md](docs/SECURITY.md) ·
  [CONTEXT.md](CONTEXT.md) · [docs/WORKFLOW.md](docs/WORKFLOW.md) · [docs/adr/](docs/adr/README.md)
