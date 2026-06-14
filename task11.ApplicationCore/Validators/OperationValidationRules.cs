namespace task11.ApplicationCore.Validators;

internal static class OperationValidationRules
{

    public const string CurrencyPattern = "^[A-Z]{3}$";

    public static readonly DateTime MinDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static bool HasAtMostTwoDecimals(decimal amount) =>
        decimal.Round(amount, 2) == amount;

    public static bool IsWithinAllowedWindow(DateTime date)
    {
        DateTime utc = date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        return utc >= MinDate && utc <= DateTime.UtcNow.AddDays(1);
    }
}
