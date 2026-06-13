namespace task11.Data.Entities;

/// <summary>
/// Base type for all persisted entities. Carries the app-generated identity,
/// audit timestamps and the soft-delete flags enforced by the global query filter.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>App-generated primary key (no DB round-trip).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Set on insert by the audit interceptor. Always UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set on update by the audit interceptor. Always UTC when present.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Soft-delete flag; defaults to false.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Set when the row is soft-deleted. Always UTC when present.</summary>
    public DateTime? DeletedAtUtc { get; set; }
}
