# CODEBASE_MAP — SLT.Manage

Where things live, and "to add X, edit Y". Read this before exploring.

## Projects

| Project | SDK | Role | Namespace |
|---|---|---|---|
| `SLT.Manage` | `Microsoft.NET.Sdk.Web` | Host/API: controllers, `Program.cs` pipeline, host-level config/middleware | `SLT.Manage.Controllers.V1`; host helpers under `SLT.Api.Utilities.*` (residue) |
| `SLT.Services` | class lib | `_<Module>` feature services + DTOs (business logic) | `SLT.Services._<Module>` |
| `SLT.Domain` | class lib | Mongo collections + repositories | `SLT.Domain.Collections` / `SLT.Domain.Repositories` |
| `SLT.Utilities` | class lib | All cross-cutting framework infra (no project refs) | prefix-less `Utilities.*` |

Solution: `SLT.Manage.slnx` (modern XML `.slnx`, not `.sln`).

## SLT.Manage (host)

| File | Purpose |
|---|---|
| `Program.cs` | Bootstrap + middleware pipeline order + Autofac wiring (`AddServices` + `AddControllerServices`) |
| `Controllers/V1/AuthController.cs` | `POST api/v1/Auth/LoginAsync` (admin login). Commented-out `CreateAdminAsync` seed endpoint |
| `Controllers/V1/ReportController.cs` | 8 read endpoints, all `[Authorize(Permissions.Reporter)]` |
| `Utilities/Configurations/ControllerAutofacConfigurationExtensions.cs` | `AddControllerServices()` — scans `SLT.Domain` + `SLT.Services` assemblies |
| `Utilities/Configurations/ControllerServiceCollectionExtensions.cs` | `AddSettings()` — currently an empty stub |
| `Utilities/Middlewares/*` | Host middlewares: RequestLogging, JwtBlacklist, ProductionCors (SecurityStamp exists but is commented-out / not wired — dead) |
| `appsettings.json` | Config + **committed secrets** (see docs/SECURITY.md). Includes unbound `AvailableTokensSettings` |
| `WeatherForecast.cs` | **Dead scaffold leftover** — unreferenced, safe to ignore/delete |

## SLT.Services (business modules — `_<Module>` pattern)

| Module | Key files |
|---|---|
| `_User/` | `IUserService.cs` / `UserService.cs` (login flow), `DTOs/LoginUpdate.cs`, `DTOs/Storages/JwtBlacklistStorage.cs` |
| `_Report/` | `IReportService.cs` / `ReportService.cs`, `DTOs/{HashUpdate, OrderFullResult, SingleWalletListResult, SingleWalletOverviewResult, TotalOverview, WalletTotalReportResult}.cs` |
| `_Log/` | `ILogService.cs` / `LogService.cs` (hard-delete retention), `DTOs/Updates/{LogUpdate, RequestLogUpdate}.cs` |

## SLT.Domain (data)

| Collections (`Collections/`) | Repositories (`Repositories/` + `Repositories/Contracts/`) |
|---|---|
| `User` ("Users"), `Order` ("Orders"), `Invoice` ("Invoices"), `TransactionLog` ("TransactionLogs"), `Log`, `RequestLog` | `I<Name>Repository` (usually empty marker) + `<Name>Repository` per collection |

Entities subclass `BaseDocument` and are tagged `[MonjoCollectionName("...")]`. Inline enums live in
the same file as the entity (e.g. `Order.cs` → `OrderType`/`OrderState`; `User.cs` → `UserStatus`/`UserRole`).

## SLT.Utilities (framework infra)

Folder = role. Highlights: `MongoDatabase/` (the Monjo wrapper: `MonjoRepository.cs`, `MonjoConnection.cs`,
`Documents/BaseDocument.cs`, `Filter/`), `Filters/` (`ApiResultFilterAttribute`, custom `AuthorizeAttribute`),
`Middlewares/` (`CustomExceptionHandlerMiddleware`, `JwtMiddleware`, `SignatureMiddleware`, `FirewallMiddleware`),
`Services/` (`JwtService`, `PasswordService`, `SignatureService`, `NonceService`), `Models/Results/ApiResult.cs`,
`Exceptions/`, `Permissions/Permissions.cs`, `Enums/{ApiResultStatusCode, Claims}.cs`, `Constants/RegisterMode.cs`,
`Configuration/` (DI + Swagger + pipeline extensions). Full inventory in `docs/ARCHITECTURE.md`.

## To add X, edit Y

| Task | Edit |
|---|---|
| New Mongo entity | `SLT.Domain/Collections/<Name>.cs` (`: BaseDocument`, `[MonjoCollectionName("...")]`) + a repo pair below |
| New repository | `SLT.Domain/Repositories/Contracts/I<Name>Repository.cs` (`: IMonjoRepository<<Name>>`) + `<Name>Repository.cs` (`: MonjoRepository<<Name>>, I<Name>Repository, ISingletonDependency`); indexes in overridden `Configure()` |
| New business service | `SLT.Services/_<Module>/I<X>Service.cs` + `<X>Service.cs` (`: I<X>Service, IScopedDependency`, primary-ctor repo injection) + `DTOs/` |
| New endpoint | Add a `[HttpPost("[action]")]` method to the relevant `Controllers/V1/*.cs`; return the bare DTO (the `[ApiResultFilter]` envelopes it) |
| New permission gate | Add a code const to `SLT.Utilities/Permissions/Permissions.cs`, gate with `[Authorize(Permissions.X)]` |
| New config POCO | Define `<Name>Settings` in `SLT.Utilities/Models/Settings/` (or `Constants/`), bind via `RegisterSetting<T>` in `AddCodeAssistantSettings`; inject the **plain class** |
| New class in a NEW assembly | Add its assembly to the `Assembly[]` arrays in `AddServices()` / `AddControllerServices()` — marker scanning only covers the three scanned assemblies |
