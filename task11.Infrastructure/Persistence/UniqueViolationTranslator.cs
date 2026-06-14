using Microsoft.EntityFrameworkCore;
using Npgsql;
using task11.ApplicationCore;

namespace task11.Infrastructure.Persistence;

internal static class UniqueViolationTranslator
{
    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public static async Task SaveChangesTranslatingUniqueViolationAsync(
        AppDbContext ctx,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException(conflictMessage);
        }
    }
}
