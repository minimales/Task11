namespace task11.ApplicationCore.Currency;

/// <summary>
/// Converts amounts between ISO-4217 currencies using historical (date-specific) rates.
/// Implemented by the Currency module so that other services can depend on it independently.
/// </summary>
public interface ICurrencyConverter
{
    /// <summary>
    /// Returns the exchange rate to convert 1 unit of <paramref name="from"/> into
    /// <paramref name="to"/> on the given <paramref name="date"/> (UTC). For same-currency
    /// pairs the rate is 1.
    /// </summary>
    /// <exception cref="FxUnavailableException">
    /// Thrown when a rate cannot be obtained after retries.
    /// </exception>
    Task<decimal> GetRateAsync(string from, string to, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts <paramref name="amount"/> from <paramref name="from"/> to <paramref name="to"/>
    /// on the given <paramref name="date"/>, rounding to 2 decimals (banker's rounding).
    /// Returns the converted amount and the rate used.
    /// </summary>
    /// <exception cref="FxUnavailableException">
    /// Thrown when a rate cannot be obtained after retries.
    /// </exception>
    Task<(decimal Converted, decimal Rate)> ConvertAsync(
        decimal amount,
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default);
}
