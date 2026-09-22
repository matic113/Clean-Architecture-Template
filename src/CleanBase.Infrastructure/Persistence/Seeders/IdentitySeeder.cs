using CleanBase.Application.Common.Constants;
using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanBase.Infrastructure.Persistence.Seeders;

public class IdentitySeeder(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger
)
{
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedDefaultAdminAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            AppRoles.Admin,
            AppRoles.User,
        };

        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new AppRole { Name = role });
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
    }

    private async Task SeedDefaultAdminAsync()
    {
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("Admin credentials are not configured. Skipping default admin seeding.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
            return;

        var admin = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User"
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create default admin user {Email}: {Errors}", adminEmail,
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign admin role to user {Email}: {Errors}", adminEmail,
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
