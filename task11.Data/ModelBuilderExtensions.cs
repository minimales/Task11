using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using task11.Data.Entities;

namespace task11.Data;

/// <summary>
/// Model-building helpers applied during <see cref="DbContext.OnModelCreating"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds a <c>HasQueryFilter(e =&gt; !e.IsDeleted)</c> to every entity that derives from
    /// <see cref="BaseEntity"/>, so soft-deleted rows are hidden from all reads.
    /// </summary>
    public static void ApplySoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(clrType, "e");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);

            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
