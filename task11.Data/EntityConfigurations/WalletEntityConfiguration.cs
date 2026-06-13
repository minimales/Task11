using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using task11.Data.Entities;

namespace task11.Data.EntityConfigurations;

public class WalletEntityConfiguration : IEntityTypeConfiguration<WalletEntity>
{
    public void Configure(EntityTypeBuilder<WalletEntity> builder)
    {
        builder.ToTable("wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.BaseCurrency)
            .IsRequired()
            .IsFixedLength()
            .HasMaxLength(3)
            .HasColumnType("char(3)")
            .HasDefaultValue("UAH");

        builder.HasIndex(w => w.OwnerUserId);

        builder.HasMany(w => w.Operations)
            .WithOne(o => o.Wallet)
            .HasForeignKey(o => o.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.OperationTypes)
            .WithOne(t => t.Wallet)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
