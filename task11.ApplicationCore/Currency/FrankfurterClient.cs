using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace task11.ApplicationCore.Currency;

/// <summary>
/// Typed <see cref="HttpClient"/> over the Frankfurter API (ECB-sourced historical rates, no key).
/// Issues <c>GET /v1/{yyyy-MM-dd}?base={from}&amp;symbols={to}</c> and parses <c>rates[to]</c>.
/// Weekend/holiday dates resolve to the most recent prior business day's rate.
/// </summary>
public sealed class FrankfurterClient
{
    /// <summary>Shape of the Frankfurter response (<c>{ amount, base, date, rates: { CUR: 1.23 } }</c>).</summary>
    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("base")]
        public string? Base { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public FrankfurterClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetches the exchange rate to convert 1 unit of <paramref name="from"/> into
    /// <paramref name="to"/> on the given <paramref name="date"/>.
    /// </summary>
    /// <exception cref="FxUnavailableException">
    /// Thrown when the request fails (transient errors are retried by the Polly handler first),
    /// the response cannot be parsed, or the requested target currency is absent from the payload.
    /// </exception>
    public async Task<decimal> GetRateAsync(
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        // Frankfurter is keyed by exact calendar date; the request path is date-only.
        var datePath = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var requestUri = $"/v1/{datePath}?base={Uri.EscapeDataString(from)}&symbols={Uri.EscapeDataString(to)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new FxUnavailableException(
                $"Failed to reach the FX provider for {from}->{to} on {datePath}.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout (not caller-initiated cancellation).
            throw new FxUnavailableException(
                $"The FX provider timed out for {from}->{to} on {datePath}.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new FxUnavailableException(
                $"The FX provider returned {(int)response.StatusCode} for {from}->{to} on {datePath}.");
        }

        FrankfurterResponse? payload;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            payload = await JsonSerializer.DeserializeAsync<FrankfurterResponse>(
                stream, _serializerOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new FxUnavailableException(
                $"The FX provider returned an unparsable response for {from}->{to} on {datePath}.", ex);
        }

        if (payload?.Rates is null || !payload.Rates.TryGetValue(to, out var rate))
        {
            throw new FxUnavailableException(
                $"The FX provider did not return a rate for {from}->{to} on {datePath}.");
        }

        if (rate <= 0m)
        {
            throw new FxUnavailableException(
                $"The FX provider returned a non-positive rate for {from}->{to} on {datePath}.");
        }

        return rate;
    }
}
