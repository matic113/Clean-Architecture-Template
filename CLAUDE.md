# Backend conventions (CleanBase API)

The authoritative guidelines, architectural blueprint, and coding instructions for this repository are consolidated in **[`AGENTS.md`](./AGENTS.md)**.
When implementing or extending features, refer to the step-by-step workspace skill in **[`.agents/skills/cleanbase-feature/SKILL.md`](./.agents/skills/cleanbase-feature/SKILL.md)**.

## Quick Summary

- **Solution**: `CleanBase.slnx` (.NET 10, Clean Architecture)
- **Domain**: Encapsulated rich entities (UUIDv7 client-side, private setters, static `Create`, instance `Update`)
- **Results**: `ErrorOr<T>` for business outcomes (no throwing for expected failures)
- **Persistence**: EF Core + PostgreSQL, automatic `snake_case` naming, auto-discovered configurations
- **API**: FastEndpoints with nested DTO + FluentValidation validator + Endpoint per file
- **Tooling**: `just build`, `just run`, `just migrate-add <Name>`, `just migrate-update`

See **[`AGENTS.md`](./AGENTS.md)** for full conventions and code blueprints.
