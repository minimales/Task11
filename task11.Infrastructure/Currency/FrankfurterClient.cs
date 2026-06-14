using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using task11.ApplicationCore;

namespace task11.Infrastructure.Currency;

public class FrankfurterClient
{

    private class FrankfurterResponse
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
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public async Task<decimal> GetRateAsync(
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);

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
