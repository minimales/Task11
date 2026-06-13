namespace task11.ApplicationCore.Validators;

/// <summary>
/// Shared validation predicates for operation request models (amount precision,
/// date window, currency code) so the create/update validators stay consistent.
/// </summary>
internal static class OperationValidationRules
{
    /// <summary>ISO-4217-shaped currency code: exactly three upper-case letters.</summary>
    public const string CurrencyPattern = "^[A-Z]{3}$";

    /// <summary>Earliest accepted operation date (inclusive).</summary>
    public static readonly DateTime MinDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>True when the amount has at most two decimal places.</summary>
    public static bool HasAtMostTwoDecimals(decimal amount) =>
        decimal.Round(amount, 2) == amount;

    /// <summary>
    /// True when the date is on/after 2000-01-01 and no more than one day in the future
    /// (compared in UTC, regardless of inbound <see cref="DateTimeKind"/>).
    /// </summary>
    public static bool IsWithinAllowedWindow(DateTime date)
    {
        var utc = date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        return utc >= MinDate && utc <= DateTime.UtcNow.AddDays(1);
    }
}
