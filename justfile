set dotenv-load

startup := "src/CleanBase.Api"
infra   := "src/CleanBase.Infrastructure"
sln     := "CleanBase.slnx"

# Restore dependencies
restore:
    dotnet restore {{sln}}

# Build the solution
build:
    dotnet build {{sln}}

# Run the API
run:
    dotnet run --project {{startup}} --launch-profile https

# Run all tests
test:
    dotnet test {{sln}}

# Add a new EF Core migration (usage: just migrate-add MigrationName)
migrate-add name:
    dotnet ef migrations add {{name}} --project {{infra}} --startup-project {{startup}} --output-dir "Persistence/Migrations"

# Update database to the latest migration
migrate-update:
    dotnet ef database update --project {{infra}} --startup-project {{startup}}

# Update database to a specific migration (usage: just migrate-to MigrationName)
migrate-to target:
    dotnet ef database update {{target}} --project {{infra}} --startup-project {{startup}}

# Remove the last migration (without applying to database)
migrate-remove:
    dotnet ef migrations remove --project {{infra}} --startup-project {{startup}}

# List all migrations
migrate-list:
    dotnet ef migrations list --project {{infra}} --startup-project {{startup}}

# Drop the database (dangerous!)
migrate-drop:
    dotnet ef database drop --project {{infra}} --startup-project {{startup}}

# Generate a SQL script for migrations (usage: just migrate-script [from..to])
migrate-script from to:
    dotnet ef migrations script {{from}} {{to}} --project {{infra}} --startup-project {{startup}} --output migration.sql

# Format code with CSharpier
fmt:
    dotnet csharpier format .

# Check formatting without changes
fmt-check:
    dotnet csharpier check .

# Clean build artifacts
clean:
    dotnet clean {{sln}}

# Build Docker container image
docker-build:
    docker build -t cleanbase .
