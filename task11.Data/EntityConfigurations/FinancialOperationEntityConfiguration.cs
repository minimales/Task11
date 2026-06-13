using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using task11.Data.Entities;

namespace task11.Data.EntityConfigurations;

/// <summary>EF mapping for <see cref="FinancialOperationEntity"/>.</summary>
public sealed class FinancialOperationEntityConfiguration : IEntityTypeConfiguration<FinancialOperationEntity>
{
    public void Configure(EntityTypeBuilder<FinancialOperationEntity> builder)
    {
        builder.ToTable("financial_operations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Amount)
            .HasPrecision(18, 2);

        builder.Property(o => o.OccurredAtUtc)
            .IsRequired();

        builder.Property(o => o.Note)
            .HasMaxLength(1000);

        // Fast scoped queries and report ranges.
        builder.HasIndex(o => new { o.WalletId, o.OccurredAtUtc });

        builder.HasIndex(o => o.OperationTypeId);
    }
}
