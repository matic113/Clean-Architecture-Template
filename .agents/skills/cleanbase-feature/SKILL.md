---
name: cleanbase-feature
description: >-
  Scaffold or implement new features and aggregates in CleanBase following the encapsulated
  rich domain, Clean Architecture (.NET 10), FastEndpoints, and ErrorOr conventions.
---

# CleanBase Feature Scaffolding & Architecture Blueprint

When adding or modifying a business feature in CleanBase, follow this exact 4-layer Clean Architecture blueprint. CleanBase groups code by **feature/aggregate**, not by technical layer folders.

---

## 1. Domain Layer (`CleanBase.Domain/<Feature>/`)

Entities must be **rich and encapsulated**. Lifecycle behavior belongs inside the entity, not in services or anemic data holders.

### Conventions:
- Primary key is `Guid Id { get; private set; }`. Use `Guid.CreateVersion7()` for time-ordered UUIDv7 generated client-side.
- Private parameterless constructor `private <Entity>() { }` for EF Core materialization.
- Properties have `private set;`.
- Expose a static factory `public static <Entity> Create(...)` that stamps `Id = Guid.CreateVersion7()`, `CreatedAt = DateTime.UtcNow`, and `UpdatedAt = DateTime.UtcNow`.
- Expose mutating methods like `public void Update(...)` that mutate fields and bump `UpdatedAt = DateTime.UtcNow`.
- Services call `Entity.Create(...)` and `entity.Update(...)` — **never** object initializers with public setters.

### Blueprint: `src/CleanBase.Domain/<Feature>/<Entity>.cs`
```csharp
namespace CleanBase.Domain.<Feature>;

public class Item
{
    private Item() { } // EF Core materialization

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Item Create(Guid userId, string title, string? description)
    {
        var now = DateTime.UtcNow;
        return new Item
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Title = title.Trim(),
            Description = description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string? description)
    {
        Title = title.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Domain Errors: `src/CleanBase.Domain/Common/Errors/Errors.<Feature>.cs`
Errors extend the `static partial class Errors` using `ErrorOr`:
```csharp
namespace CleanBase.Domain.Common.Errors;

public static partial class Errors
{
    public static class Item
    {
        public static Error NotFound => Error.NotFound(
            code: "Item.NotFound",
            description: "The requested item was not found."
        );

        public static Error DuplicateTitle => Error.Conflict(
            code: "Item.DuplicateTitle",
            description: "An item with this title already exists."
        );
    }
}
```

---

## 2. Application Layer (`CleanBase.Application/<Feature>/`)

Business logic, orchestration, and query projection live here. Endpoints stay thin and delegate to an `I<Feature>Service`.

### Conventions:
- Return `ErrorOr<T>` (never throw for expected business errors).
- Take `Guid userId` for user-owned operations and enforce ownership in the query predicate (`x => x.Id == id && x.UserId == userId`).
- **High-Performance EF Projections**: For queries, use an `Expression<Func<TEntity, TDto>>` selector compiled for single-item mapping.
- Register services via a per-feature `DependencyInjection.cs` (`Add<Feature>Services()`), called from `CleanBase.Application/DependencyInjection.cs`.

### DTOs: `src/CleanBase.Application/<Feature>/DTOs/`
```csharp
namespace CleanBase.Application.<Feature>.DTOs;

public record ItemResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateItemRequest(string Title, string? Description);
public record UpdateItemRequest(string Title, string? Description);
```

### Interface & Service: `src/CleanBase.Application/<Feature>/`
```csharp
namespace CleanBase.Application.<Feature>.Interfaces;

public interface IItemService
{
    Task<ErrorOr<ItemResponse>> CreateAsync(Guid userId, CreateItemRequest request, CancellationToken ct = default);
    Task<ErrorOr<ItemResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<ErrorOr<PagedList<ItemResponse>>> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<ErrorOr<ItemResponse>> UpdateAsync(Guid userId, Guid id, UpdateItemRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
```

```csharp
using System.Linq.Expressions;
using CleanBase.Application.Common.Abstractions;
using CleanBase.Application.<Feature>.DTOs;
using CleanBase.Application.<Feature>.Interfaces;
using CleanBase.Domain.Common.Errors;
using CleanBase.Domain.<Feature>;
using Microsoft.EntityFrameworkCore;

namespace CleanBase.Application.<Feature>.Services;

public class ItemService(IUnitOfWork unitOfWork) : IItemService
{
    private static readonly Expression<Func<Item, ItemResponse>> ToResponse = item =>
        new ItemResponse(item.Id, item.Title, item.Description, item.CreatedAt, item.UpdatedAt);

    private static readonly Func<Item, ItemResponse> Map = ToResponse.Compile();

    public async Task<ErrorOr<ItemResponse>> CreateAsync(Guid userId, CreateItemRequest request, CancellationToken ct = default)
    {
        var repo = unitOfWork.Repository<Item>();
        var item = Item.Create(userId, request.Title, request.Description);

        await repo.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Map(item);
    }

    public async Task<ErrorOr<ItemResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await unitOfWork.Repository<Item>().GetQueryable()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(ToResponse)
            .FirstOrDefaultAsync(ct);

        return item is not null ? item : Errors.Item.NotFound;
    }

    public async Task<ErrorOr<PagedList<ItemResponse>>> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = unitOfWork.Repository<Item>().GetQueryable()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);

        return await PagedList<ItemResponse>.CreateAsync(query, ToResponse, page, pageSize, ct);
    }

    public async Task<ErrorOr<ItemResponse>> UpdateAsync(Guid userId, Guid id, UpdateItemRequest request, CancellationToken ct = default)
    {
        var repo = unitOfWork.Repository<Item>();
        var item = await repo.GetQueryable()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

        if (item is null)
            return Errors.Item.NotFound;

        item.Update(request.Title, request.Description);
        // Do NOT call repo.Update() for tracked entities — EF detects changes automatically
        await unitOfWork.SaveChangesAsync(ct);

        return Map(item);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var repo = unitOfWork.Repository<Item>();
        var item = await repo.GetQueryable()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

        if (item is null)
            return Errors.Item.NotFound;

        repo.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success;
    }
}
```

### Application Dependency Injection:
```csharp
namespace CleanBase.Application.<Feature>;

public static class DependencyInjection
{
    public static IServiceCollection AddItemServices(this IServiceCollection services)
    {
        services.AddScoped<IItemService, ItemService>();
        return services;
    }
}
```
*Remember to invoke `services.AddItemServices()` in `CleanBase.Application/DependencyInjection.cs`.*

---

## 3. Infrastructure Layer (`CleanBase.Infrastructure/Persistence/`)

- Add `DbSet<<Entity>> <Entities> { get; set; }` to `ApplicationDbContext.cs`.
- Create a dedicated configuration in `Persistence/Configurations/<Entity>Configuration.cs`.
- Configurations are auto-discovered via `ApplyConfigurationsFromAssembly`.
- Snake_case table and column names are automatic (`UseSnakeCaseNamingConvention`).
- For jsonb columns, use the `.HasJsonbConversion()` extension.

### Blueprint: `src/CleanBase.Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`
```csharp
using CleanBase.Domain.<Feature>;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanBase.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
```

---

## 4. API Layer (`CleanBase.Api/Features/<Feature>/`)

CleanBase uses **FastEndpoints**.
- One endpoint class per file (e.g. `Create.cs`, `Get.cs`, `List.cs`, `Update.cs`, `Delete.cs`).
- Inside the file, nest:
  1. Outer class: `public class Create`
  2. Request DTO: `public sealed record Request(...)`
  3. FluentValidation: `public sealed class Validator : Validator<Request>`
  4. Endpoint: `public sealed class Endpoint : Endpoint<Request, ApiSuccessResponse<ItemResponse>>`
- All endpoints require JWT authentication by default (omit `AllowAnonymous()`).
- Retrieve caller identity via `User.GetUserIdAsGuid()`.
- Return responses using `this.SendResultAsync(result, map, ct)` or `this.SendNoContentResultAsync(result, ct)`.

### Blueprint: `src/CleanBase.Api/Features/<Feature>/Create.cs`
```csharp
using CleanBase.Application.<Feature>.DTOs;
using CleanBase.Application.<Feature>.Interfaces;
using FastEndpoints;
using FluentValidation;

namespace CleanBase.Api.Features.<Feature>;

public class Create
{
    public sealed record Request(string Title, string? Description);

    public sealed class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
        }
    }

    public sealed class Endpoint(IItemService service)
        : Endpoint<Request, ApiSuccessResponse<ItemResponse>>
    {
        public override void Configure()
        {
            Post("/items");
            Description(b => b
                .WithDescription("Creates a new item.")
                .WithTags("Items"));
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var userId = User.GetUserIdAsGuid();
            var result = await service.CreateAsync(
                userId,
                new CreateItemRequest(req.Title, req.Description),
                ct
            );

            await Send.OkAsync(result, ct);
        }
    }
}
```

---

## 5. Tooling & Migrations Workflow

After modifying domain models and configurations:
1. `just build` — Build solution.
2. `just migrate-add <MigrationName>` — Create EF Core migration.
3. `dotnet ef migrations has-pending-model-changes --project src/CleanBase.Infrastructure --startup-project src/CleanBase.Api` — Confirm no schema drift.
4. `just migrate-update` — Apply migration to database.
