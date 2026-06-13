using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using task11.Data.Entities;

namespace task11.Data.EntityConfigurations;

/// <summary>EF mapping for <see cref="OperationTypeEntity"/>.</summary>
public sealed class OperationTypeEntityConfiguration : IEntityTypeConfiguration<OperationTypeEntity>
{
    public void Configure(EntityTypeBuilder<OperationTypeEntity> builder)
    {
        builder.ToTable("operation_types");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Kind)
            .IsRequired()
            .HasConversion<int>();

        // Type names are unique per wallet among non-deleted rows.
        builder.HasIndex(t => new { t.WalletId, t.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(t => t.Operations)
            .WithOne(o => o.OperationType)
            .HasForeignKey(o => o.OperationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
