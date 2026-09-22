# CleanBase Agent Guidelines

This repository is **CleanBase**, a production-ready, enterprise-grade Clean Architecture backend template in .NET 10.

---

## 1. Solution & Architecture Layout

Solution file: `CleanBase.slnx` (modern XML format).

```
CleanBase.slnx
├── src/CleanBase.Domain/          # Encapsulated rich entities, domain errors, enums
├── src/CleanBase.Application/     # Services, DTOs, interfaces, result envelopes (ErrorOr)
├── src/CleanBase.Infrastructure/  # EF Core, PostgreSQL, S3/MinIO, ImageSharp, OpenRouter, Firebase, Hangfire
└── src/CleanBase.Api/             # FastEndpoints, middleware, auth extraction, Scalar OpenAPI
```

Group code by **feature/aggregate**, not by technical type:
- `src/CleanBase.Domain/<Feature>/`
- `src/CleanBase.Application/<Feature>/{DTOs,Interfaces,Services}` (e.g. `src/CleanBase.Application/Identity/{Authentication,AccountDeletion}`)
- `src/CleanBase.Api/Features/<Feature>/` (e.g. `src/CleanBase.Api/Features/Identity/{Authentication,AccountDeletion}`)
- `src/CleanBase.Infrastructure/Persistence/Configurations/<Feature>Configuration.cs`

When scaffolding new features, follow the workspace skill: **[`.agents/skills/cleanbase-feature/SKILL.md`](./.agents/skills/cleanbase-feature/SKILL.md)**.

---

## 2. Domain Model Conventions (Rich & Encapsulated)

Never write anemic domain models with public setters. Lifecycle behavior belongs **inside the entity**:

- Primary key is `Guid Id { get; private set; }`. Use client-side time-ordered UUIDv7: `Guid.CreateVersion7()`.
- Add a private parameterless constructor `private <Entity>() { }` for EF Core materialization.
- Properties must have `private set;`.
- Expose a `public static <Entity> Create(...)` factory that generates the UUIDv7 `Id` and stamps `CreatedAt` and `UpdatedAt` in UTC.
- Expose mutating methods (e.g. `public void Update(...)`) that mutate fields and bump `UpdatedAt = DateTime.UtcNow`.
- Services call `Entity.Create(...)` and `entity.Update(...)` — **never** object initializers with public setters.

---

## 3. Errors & Results (`ErrorOr`)

- Return `ErrorOr<T>` from application and domain services — **never throw exceptions for expected business logic failures**.
- Errors are declared in `static partial class Errors`, one partial file per aggregate:
  `src/CleanBase.Domain/Common/Errors/Errors.<Aggregate>.cs` with a nested `public static class <Aggregate>`.
- Use `Error.NotFound`, `Error.Conflict`, `Error.Validation`, `Error.Failure` — these map directly to HTTP status codes via `ApiErrorResponse.FromErrors`.

---

## 4. Application Services

- Endpoints stay thin; orchestration and business logic live in an `I<Feature>Service` + `<Feature>Service`.
- For user-owned resources, accept `Guid userId` and enforce ownership in the query predicate:
  `c => c.Id == id && c.UserId == userId` so an unauthorized caller gets `NotFound` rather than leaking existence.
- **High-Performance EF Projections**:
  - Define projection expressions once: `static readonly Expression<Func<Entity, Dto>> ToResponse = ...;`
  - Derive compiled delegate for single-entity paths: `static readonly Func<Entity, Dto> Map = ToResponse.Compile();`
  - Use `PagedList<TDto>.CreateAsync(query, ToResponse, page, pageSize, ct)` for untracked, SQL-projected paging.
- Register each feature's services via a per-feature `DependencyInjection.cs` (`Add<Feature>Services()`), called from `CleanBase.Application/DependencyInjection.cs`.

---

## 5. Data Access (EF Core & PostgreSQL)

- Use `unitOfWork.Repository<T>()` and call `unitOfWork.SaveChangesAsync(ct)` separately.
- **Do not call `repo.Update(entity)` for a tracked entity** (one loaded in the same context). EF Core change tracking detects mutations automatically and generates targeted UPDATE statements of only changed columns. `repo.Update` is only for detached entities.
- Table and column names are automatically converted to `snake_case` (`UseSnakeCaseNamingConvention`). Do **not** add manual `ToTable` or `HasColumnName` mappings.
- Store enums as readable text: `builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50)`.
- Use the `.HasJsonbConversion()` extension (`JsonbConversionExtensions.cs`) for complex objects or JSON list columns. It configures the PostgreSQL `jsonb` type and a content-based `ValueComparer`.
- One `IEntityTypeConfiguration<T>` per entity in `Persistence/Configurations/`, auto-discovered via `ApplyConfigurationsFromAssembly`.
- Add a corresponding `DbSet<T>` to `ApplicationDbContext`.

---

## 6. API Layer (FastEndpoints)

- One endpoint class per file under `src/CleanBase.Api/Features/<Area>/` (e.g. `Create.cs`, `Get.cs`, `List.cs`, `Update.cs`, `Delete.cs`).
- Inside each file, nest:
  - `public sealed record <Action>Request(...)`
  - `public sealed class Validator : Validator<<Action>Request>` (FluentValidation)
  - `public sealed class Endpoint : Endpoint<<Action>Request, ApiSuccessResponse<<ResponseDto>>>`
- Respond using idiomatic FastEndpoints syntax via `CleanBase.Api.Common.EndpointExtensions`:
  - `await Send.OkAsync(result, ct);` (or with a custom mapper `await Send.OkAsync(result, map, ct);`)
  - `await Send.NoContentAsync(result, ct);`
  - (`await Send.ResultAsync(...)` and `this.SendResultAsync(...)` are also supported)
- Route prefix is `api` (e.g. `Post("/items")` serves `/api/items`).
- **Auth by default**: omit `AllowAnonymous()` to require a JWT. Extract authenticated user ID with `User.GetUserIdAsGuid()`.
- API request records map into Application DTOs; never pass FastEndpoints request objects into Application services.

---

## 7. Reusable Utilities

- **Image Processing**: `IImageProcessor` (`CleanBase.Application.Utilities.ImageProcessing`) — SixLabors.ImageSharp WebP compression and preset resizing.
- **Object Storage**: `IStorageService` (`CleanBase.Application.Utilities.Storage`) — S3 / MinIO / Cloudflare R2 presigned URLs, upload, and deletion.
- **AI Chat / Completions**: `OpenRouterClient` (`CleanBase.Infrastructure.Utilities.AI.OpenRouter`) — resilient OpenAI-compatible client with exponential backoff and `StripReasoningHandler`.
- **Push Notifications**: `IFirebaseNotificationService` (`CleanBase.Application.Utilities.Notifications`) — Firebase Admin FCM for device tokens, multicast, and topic broadcasts.
- **Background Jobs**: Hangfire dashboard at `/hangfire` with basic auth.
- **Email**: FluentEmail with Razor templates.

---

## 8. Tooling Recipes (`justfile`)

```bash
just build              # Build solution (CleanBase.slnx)
just run                # Run API (Scalar UI at /scalar/v1)
just migrate-add <Name> # Add an EF Core migration
just migrate-update     # Apply migrations
just migrate-remove     # Undo last unapplied migration
```

After modifying entity configurations, verify schema synchronization:
```bash
dotnet ef migrations has-pending-model-changes --project src/CleanBase.Infrastructure --startup-project src/CleanBase.Api
```
