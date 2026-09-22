# CleanBase

A production-ready, enterprise-grade **Clean Architecture** backend template built with **.NET 10** and **FastEndpoints**.

CleanBase is designed as a solid, battle-tested foundation for SaaS products, mobile app backends, and modern web APIs. It eliminates repetitive setup by providing out-of-the-box authentication, rich domain encapsulation, background processing, cloud storage, AI integrations, and push notifications while maintaining strict layer decoupling and high performance.

---

## Tech Stack & Highlights

- **Framework**: [.NET 10 Web API](https://dotnet.microsoft.com/)
- **API Framework**: [FastEndpoints](https://fast-endpoints.com/) (REPR pattern with nested DTO + FluentValidation)
- **Architecture**: Clean Architecture with encapsulated rich domain aggregates (UUIDv7, zero public setters)
- **Database**: PostgreSQL with [EF Core](https://learn.microsoft.com/ef/core/) and automatic `snake_case` conventions
- **Caching**: Redis with .NET 10 `HybridCache`
- **Identity & Security**: ASP.NET Core Identity with stateless JWTs, refresh tokens, OTP email verification, token blacklisting, and Google/Apple OAuth
- **Background Jobs**: [Hangfire](https://www.hangfire.io/) with PostgreSQL storage and basic-auth dashboard
- **AI Integrations**: [OpenRouter](https://openrouter.ai/) client with exponential retry and reasoning-stripping middleware
- **Push Notifications**: [Firebase Admin SDK](https://firebase.google.com/docs/admin/setup) (FCM device, multicast, and topic notifications)
- **Media & Storage**: [SixLabors.ImageSharp](https://sixlabors.com/imagesharp/) (WebP presets and compression) + S3-compatible storage (MinIO / AWS S3 / Cloudflare R2)
- **Emails**: [FluentEmail](https://github.com/lukesampson/fluent-email) with Razor templates
- **Documentation**: OpenAPI 3.1 + [Scalar API Reference UI](https://scalar.com/)

---

## Solution Structure

CleanBase uses the modern `.slnx` XML solution format:

```
CleanBase.slnx
├── src/docs/                      # Markdown documentation
├── src/
│   ├── CleanBase.Domain/          # Encapsulated rich entities, domain errors, enums
│   ├── CleanBase.Application/     # Use cases, services, DTOs, interfaces, ErrorOr results
│   ├── CleanBase.Infrastructure/  # EF Core, PostgreSQL, S3, ImageSharp, OpenRouter, Firebase, Hangfire
│   └── CleanBase.Api/             # FastEndpoints, middleware, auth extraction, Scalar UI
├── .agents/
│   └── skills/cleanbase-feature/  # Workspace skill for scaffolding new features
├── AGENTS.md                      # Authoritative agent rules and conventions
├── CLAUDE.md                      # Quick conventions reference
└── justfile                       # Development recipes
```

Features are grouped **by feature/aggregate**, not by technical types across the entire codebase.

---

## Core Features Included

### 1. Identity & Authentication (`/api/auth`)
- **Email & Password**: Registration and login with OTP email verification and password reset flows.
- **Tokens**: Stateless JWT issuance, sliding refresh token rotation, and in-memory/Redis token revocation blacklists.
- **Social OAuth**: Ready-to-use Google and Apple Sign-In verification and account linking.
- **Role Seeding**: Automated startup seeding of default roles (`Admin`, `User`) and a seeded admin user.

### 2. User Profiles & Avatars (`/api/profile`)
- User profile retrieval and detail updates.
- Profile picture upload with automatic WebP compression and dimension normalization via `IImageProcessor`, served securely via presigned S3 URLs.

### 3. Account Deletion & Retention Window (`/api/account`)
- Self-service account deletion request with emailed OTP confirmation.
- 30-day soft-deletion retention window allowing administrative restore (`/api/admin/account-deletions`).
- Hangfire daily background job (`UserPurgeService`) that sweeps and hard-deletes accounts past their retention date along with their private S3 assets.

### 4. Integrated Reusable Utilities
- **Image Processing (`IImageProcessor`)**: High-performance image transcoding to WebP with predefined size presets (`Size1000`, `Size500`, `Size200`).
- **Object Storage (`IStorageService`)**: S3-compatible abstraction for uploading, generating presigned URLs, and deleting assets across public and private buckets.
- **AI Chat & Completions (`OpenRouterClient`)**: Pre-wired client configured with transport resilience (exponential backoff with jitter) and a `StripReasoningHandler` for Groq/strict OpenAI-compatible providers.
- **Firebase Push Notifications (`IFirebaseNotificationService`)**: FCM client supporting single-device tokens, multicasting, and topic subscriptions.

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/) (or Docker)
- [Redis](https://redis.io/) (or Docker)
- [`just`](https://github.com/casey/just) command runner (optional, recommended)
- MinIO or S3-compatible bucket (optional, for asset storage)

### 1. Configuration
Configure connection strings and API keys in `src/CleanBase.Api/appsettings.Development.json` or using .NET user-secrets:

```bash
cd src/CleanBase.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=cleanbase-dev;Username=postgres;Password=postgres;"
dotnet user-secrets set "ConnectionStrings:RedisConnectionString" "localhost:6379,abortConnect=false"
dotnet user-secrets set "Authentication:JwtOptions:Key" "YOUR_SUPER_SECRET_SECURITY_KEY_AT_LEAST_32_CHARS_LONG"
```

### 2. Build & Database Migrations
Run using `just` from the repository root:

```bash
# Restore dependencies and build
just build

# Apply the InitialCreate EF Core migration to your database
just migrate-update

# Run the API
just run
```

Or using standard `dotnet` CLI:

```bash
dotnet build CleanBase.slnx
dotnet ef database update --project src/CleanBase.Infrastructure --startup-project src/CleanBase.Api
dotnet run --project src/CleanBase.Api --launch-profile https
```

### 3. Explore API Documentation
When running in Development:
- **Scalar API Reference**: `https://localhost:5001/scalar/v1`
- **Hangfire Dashboard**: `https://localhost:5001/hangfire` (Credentials configured under `HangfireDashboard` in `appsettings.json`)

---

## Development & Scaffolding Workflow

When building new features, follow the patterns documented in [`AGENTS.md`](./AGENTS.md). You can also instruct AI agents to use the built-in skill:

```bash
# Example prompt to an agent:
"Implement a new feature called Bookmarks following the cleanbase-feature skill."
```

### Common `just` Recipes:
- `just build` — Compile `CleanBase.slnx`.
- `just run` — Run the API project.
- `just migrate-add <Name>` — Add an EF Core migration in `src/CleanBase.Infrastructure/Persistence/Migrations/`.
- `just migrate-update` — Update the database to the latest migration.
- `just migrate-remove` — Remove the last unapplied migration.

---

## License
MIT License. Free to use for personal and commercial projects.
