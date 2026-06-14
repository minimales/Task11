using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Entities;

namespace task11.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    public static void ApplySoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type clrType = entityType.ClrType;
            if (!typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            ParameterExpression parameter = Expression.Parameter(clrType, "e");
            MemberExpression property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            UnaryExpression notDeleted = Expression.Not(property);
            LambdaExpression lambda = Expression.Lambda(notDeleted, parameter);

            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
