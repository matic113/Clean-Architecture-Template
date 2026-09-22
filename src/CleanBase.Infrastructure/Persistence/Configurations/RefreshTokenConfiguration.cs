using CleanBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanBase.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.Property(rt => rt.UserId).IsRequired();
        builder.HasIndex(rt => rt.UserId);

        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(rt => rt.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(rt => rt.CreatedAt).IsRequired();
        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.IsRevoked).IsRequired();
        builder.Property(rt => rt.RevokedAt);
    }
}
