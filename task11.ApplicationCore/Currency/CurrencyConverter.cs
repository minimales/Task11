using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using task11.Data;

namespace task11.ApplicationCore.Currency;

public class CurrencyConverter : ICurrencyConverter
{
    private const string _uah = "UAH";
    private const int _roundingDecimals = 2;
    private static readonly TimeSpan _todayRateTtl = TimeSpan.FromHours(1);

    private readonly FrankfurterClient _frankfurter;
    private readonly PrivatBankClient _privatBank;
    private readonly IMemoryCache _cache;
    private readonly IClock _clock;

    public CurrencyConverter(
        FrankfurterClient frankfurter,
        PrivatBankClient privatBank,
        IMemoryCache cache,
        IClock clock)
    {
        _frankfurter = frankfurter;
        _privatBank = privatBank;
        _cache = cache;
        _clock = clock;
    }

    public async Task<decimal> GetRateAsync(
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);

        from = from.Trim().ToUpperInvariant();
        to = to.Trim().ToUpperInvariant();

        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return 1m;
        }

        var rateDate = date.Date;
        var cacheKey = BuildCacheKey(from, to, rateDate);

        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            return cachedRate;
        }

        bool involvesUah = from == _uah || to == _uah;
        var rate = involvesUah
            ? await _privatBank.GetRateAsync(from, to, rateDate, cancellationToken)
            : await _frankfurter.GetRateAsync(from, to, rateDate, cancellationToken);

        var today = _clock.UtcNow.Date;
        if (rateDate < today)
        {
            _cache.Set(cacheKey, rate);
        }
        else
        {
            _cache.Set(cacheKey, rate, _todayRateTtl);
        }

        return rate;
    }

    public async Task<(decimal Converted, decimal Rate)> ConvertAsync(
        decimal amount,
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var rate = await GetRateAsync(from, to, date, cancellationToken);
        var converted = Math.Round(amount * rate, _roundingDecimals, MidpointRounding.ToEven);
        return (converted, rate);
    }

    private static string BuildCacheKey(string from, string to, DateTime date) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"fx:{from}:{to}:{date:yyyy-MM-dd}");
}
