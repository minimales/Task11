using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using task11.Data.Entities;

namespace task11.Data.Interceptors;

/// <summary>
/// Rewrites every <see cref="EntityState.Deleted"/> change on a <see cref="BaseEntity"/>
/// into a soft delete (<c>IsDeleted = true</c>, <c>DeletedAtUtc = now</c>). Combined with the
/// global query filter this makes a physical delete impossible anywhere in the app.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;

    public SoftDeleteInterceptor(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Rewrite(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Rewrite(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Rewrite(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTime now = _clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = now;
            entry.Entity.UpdatedAtUtc = now;
        }
    }
}
