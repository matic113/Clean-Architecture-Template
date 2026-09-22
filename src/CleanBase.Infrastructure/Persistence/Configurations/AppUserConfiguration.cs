using CleanBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanBase.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(500);

        // The daily purge job scans by PurgeScheduledAt; index it so the sweep stays cheap.
        builder.HasIndex(u => u.PurgeScheduledAt);
    }
}
