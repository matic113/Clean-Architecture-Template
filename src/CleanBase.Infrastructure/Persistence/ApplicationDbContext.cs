using CleanBase.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanBase.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ConfigureIdentity(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        // Rename Identity tables to snake_case to match PostgreSQL conventions
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        // Rename unique indexes for consistency
        builder.Entity<AppRole>()
            .HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ix_roles_normalized_name");

        builder.Entity<AppUser>()
            .HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("ix_users_normalized_email");

        builder.Entity<AppUser>()
            .HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("ix_users_normalized_user_name");
    }
}
