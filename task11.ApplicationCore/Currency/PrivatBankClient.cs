using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace task11.ApplicationCore.Currency;

public class PrivatBankClient
{

    private class PrivatBankResponse
    {
        [JsonPropertyName("date")] public string? Date { get; set; }

        [JsonPropertyName("baseCurrencyLit")] public string? BaseCurrencyLit { get; set; }

        [JsonPropertyName("exchangeRate")] public List<ExchangeRateEntry>? ExchangeRate { get; set; }
    }

    private class ExchangeRateEntry
    {
        [JsonPropertyName("currency")] public string? Currency { get; set; }

        [JsonPropertyName("saleRateNB")] public decimal SaleRateNB { get; set; }

        [JsonPropertyName("purchaseRateNB")] public decimal PurchaseRateNB { get; set; }

        [JsonPropertyName("saleRate")] public decimal? SaleRate { get; set; }

        [JsonPropertyName("purchaseRate")] public decimal? PurchaseRate { get; set; }
    }

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

            throw new FxUnavailableException(
                $"PrivatBank only converts a foreign currency against UAH (got {from}->{to}).");
        }

        string foreign = fromUah ? to : from;
        decimal uahPerForeign = await GetUahPerUnitAsync(foreign, date, cancellationToken);

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

        if (entry is null || entry.SaleRateNB <= 0m)
        {
            throw new FxUnavailableException(
                $"PrivatBank did not return an NBU rate for {currency} on {datePath}.");
        }

        return entry.SaleRateNB;
    }
}
