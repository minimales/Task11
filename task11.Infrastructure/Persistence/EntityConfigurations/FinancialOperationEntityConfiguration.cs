using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using task11.ApplicationCore.Entities;

namespace task11.Infrastructure.Persistence.EntityConfigurations;

public class FinancialOperationEntityConfiguration : IEntityTypeConfiguration<FinancialOperationEntity>
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

        builder.HasIndex(o => new { o.WalletId, o.OccurredAtUtc });

        builder.HasIndex(o => o.OperationTypeId);
    }
}
