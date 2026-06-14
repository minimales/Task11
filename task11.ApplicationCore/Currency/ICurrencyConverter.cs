namespace task11.ApplicationCore.Currency;

public interface ICurrencyConverter
{
    Task<decimal> GetRateAsync(string from, string to, DateTime date, CancellationToken cancellationToken = default);

    Task<(decimal Converted, decimal Rate)> ConvertAsync(
        decimal amount,
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default);
}
