using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using task11.Data.Entities;

namespace task11.Data.EntityConfigurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("User");

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(u => u.OwnedWallets)
            .WithOne(w => w.Owner)
            .HasForeignKey(w => w.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
