using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace task11.ApplicationCore.Currency;

/// <summary>
/// Typed <see cref="HttpClient"/> over the PrivatBank historical exchange-rate API (free, no key).
/// Issues <c>GET /p24api/exchange_rates?json&amp;date=dd.MM.yyyy</c> and reads the NBU official rate
/// (<c>saleRateNB</c>, which equals <c>purchaseRateNB</c>) — the number of UAH per 1 unit of the
/// foreign currency on that date. Used for any conversion involving UAH, which the ECB-based
/// Frankfurter feed does not publish (e.g. the task's hryvnia base-currency example).
/// </summary>
public sealed class PrivatBankClient
{
    /// <summary>Shape of the PrivatBank <c>exchange_rates</c> response.</summary>
    private sealed class PrivatBankResponse
    {
        [JsonPropertyName("date")] public string? Date { get; set; }

        [JsonPropertyName("baseCurrencyLit")] public string? BaseCurrencyLit { get; set; }

        [JsonPropertyName("exchangeRate")] public List<ExchangeRateEntry>? ExchangeRate { get; set; }
    }

    private sealed class ExchangeRateEntry
    {
        [JsonPropertyName("currency")] public string? Currency { get; set; }

        /// <summary>NBU official rate: UAH per 1 unit of <see cref="Currency"/> (== PurchaseRateNB).</summary>
        [JsonPropertyName("saleRateNB")] public decimal SaleRateNB { get; set; }

        [JsonPropertyName("purchaseRateNB")] public decimal PurchaseRateNB { get; set; }

        [JsonPropertyName("saleRate")] public decimal? SaleRate { get; set; }

        [JsonPropertyName("purchaseRate")] public decimal? PurchaseRate { get; set; }
    }

    /// <summary>ISO code of the Ukrainian hryvnia — PrivatBank's base currency.</summary>
    public const string Uah = "UAH";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public PrivatBankClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Returns the rate to convert 1 unit of <paramref name="from"/> into <paramref name="to"/> on
    /// <paramref name="date"/>. Exactly one of the two currencies must be UAH (the other any
    /// PrivatBank-listed currency).
    /// </summary>
    /// <exception cref="FxUnavailableException">
    /// Thrown on request/parse failure, when the currency is not listed for the date, or when the
    /// pair is not a UAH/foreign pair. An unconverted amount is never returned silently.
    /// </exception>
    public async Task<decimal> GetRateAsync(
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        bool fromUah = string.Equals(from, Uah, StringComparison.OrdinalIgnoreCase);
        bool toUah = string.Equals(to, Uah, StringComparison.OrdinalIgnoreCase);

        if (fromUah == toUah)
        {
            // This client only resolves a single foreign currency against UAH.
            throw new FxUnavailableException(
                $"PrivatBank only converts a foreign currency against UAH (got {from}->{to}).");
        }

        string foreign = fromUah ? to : from;
        decimal uahPerForeign = await GetUahPerUnitAsync(foreign, date, cancellationToken);

        // PrivatBank quotes UAH per 1 unit of the foreign currency:
        //   foreign -> UAH : multiply by the quote;  UAH -> foreign : divide by the quote.
        return toUah ? uahPerForeign : 1m / uahPerForeign;
    }

    private async Task<decimal> GetUahPerUnitAsync(string currency, DateTime date, CancellationToken cancellationToken)
    {
        var datePath = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var requestUri = $"/p24api/exchange_rates?json&date={datePath}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new FxUnavailableException($"Failed to reach PrivatBank for {currency}/UAH on {datePath}.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FxUnavailableException($"PrivatBank timed out for {currency}/UAH on {datePath}.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new FxUnavailableException(
                $"PrivatBank returned {(int)response.StatusCode} for {currency}/UAH on {datePath}.");
        }

        PrivatBankResponse? payload;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            payload = await JsonSerializer.DeserializeAsync<PrivatBankResponse>(
                stream, _serializerOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new FxUnavailableException(
                $"PrivatBank returned an unparsable response for {currency}/UAH on {datePath}.", ex);
        }

        var entry = payload?.ExchangeRate?.FirstOrDefault(
            r => string.Equals(r.Currency, currency, StringComparison.OrdinalIgnoreCase));

        // The NBU official rate is present for every standard currency on a banking date.
        if (entry is null || entry.SaleRateNB <= 0m)
        {
            throw new FxUnavailableException(
                $"PrivatBank did not return an NBU rate for {currency} on {datePath}.");
        }

        return entry.SaleRateNB;
    }
}
