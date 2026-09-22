using CleanBase.Infrastructure.Persistence;
using CleanBase.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanBase.Infrastructure;

public static class ConfigureApp
{
    public static async Task ConfigureInfrastructure(this WebApplication app)
    {
        await app.EnsureDatabaseCreatedAsync();
        await app.ApplySeeders();

        app.UseAuthentication();
        app.UseAuthorization();
    }

    private static async Task EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            await db.Database.MigrateAsync();
        }
    }

    private static async Task ApplySeeders(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();
    }
}