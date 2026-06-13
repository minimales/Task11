using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using task11.ApplicationCore;
using task11.ApplicationCore.Currency;
using task11.Data;

/// <summary>
/// Behavioural tests for <see cref="CurrencyConverter"/>: non-UAH pairs route to Frankfurter,
/// UAH pairs route to PrivatBank (with correct direction), the <c>(from,to,date)</c> cache prevents
/// a second upstream call, and a hard upstream failure surfaces as <see cref="FxUnavailableException"/>
/// (never a silent unconverted fallback). HTTP is faked with a hand-rolled
/// <see cref="StubHttpMessageHandler"/> (no Moq).
/// </summary>
[TestClass]
public sealed class CurrencyConverterTests
{
    private static readonly DateTime _rateDate = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    // PrivatBank exchange_rates payload: EUR quoted at 48.3734 UAH (NBU official rate) for the date.
    private const string _privatBankEurJson =
        "{\"date\":\"15.01.2024\",\"bank\":\"PB\",\"baseCurrency\":980,\"baseCurrencyLit\":\"UAH\"," +
        "\"exchangeRate\":[{\"baseCurrency\":\"UAH\",\"currency\":\"EUR\"," +
        "\"saleRateNB\":48.3734,\"purchaseRateNB\":48.3734,\"saleRate\":49.0,\"purchaseRate\":48.0}]}";

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_SameCurrency_ReturnsOne_AndMakesNoHttpCall()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Json("{}");
        CurrencyConverter sut = CreateSut(handler);

        decimal rate = await sut.GetRateAsync("USD", "USD", _rateDate);

        Assert.AreEqual(1m, rate);
        Assert.AreEqual(0, handler.CallCount);
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_NonUahPair_UsesFrankfurter_AndCacheHitAvoidsSecondCall()
    {
        StubHttpMessageHandler frankfurter = StubHttpMessageHandler.Json(
            "{\"amount\":1.0,\"base\":\"EUR\",\"date\":\"2024-01-15\",\"rates\":{\"USD\":1.1}}");
        CurrencyConverter sut = CreateSut(frankfurter);

        decimal first = await sut.GetRateAsync("EUR", "USD", _rateDate);
        decimal second = await sut.GetRateAsync("EUR", "USD", _rateDate);

        Assert.AreEqual(1.1m, first);
        Assert.AreEqual(1.1m, second);

        // Exactly one upstream call despite two GetRateAsync invocations for the same (from,to,date).
        Assert.AreEqual(1, frankfurter.CallCount);
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_ForeignToUah_UsesPrivatBankNbuRate()
    {
        StubHttpMessageHandler frankfurter = StubHttpMessageHandler.Json("{}");
        StubHttpMessageHandler privat = StubHttpMessageHandler.Json(_privatBankEurJson);
        CurrencyConverter sut = CreateSut(frankfurter, privat);

        decimal rate = await sut.GetRateAsync("EUR", "UAH", _rateDate);

        // 1 EUR = 48.3734 UAH.
        Assert.AreEqual(48.3734m, rate);

        // Frankfurter is never consulted for a UAH pair.
        Assert.AreEqual(0, frankfurter.CallCount);
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_UahToForeign_InvertsPrivatBankRate()
    {
        StubHttpMessageHandler frankfurter = StubHttpMessageHandler.Json("{}");
        StubHttpMessageHandler privat = StubHttpMessageHandler.Json(_privatBankEurJson);
        CurrencyConverter sut = CreateSut(frankfurter, privat);

        decimal rate = await sut.GetRateAsync("UAH", "EUR", _rateDate);

        // UAH -> EUR is the reciprocal of the UAH-per-EUR quote.
        Assert.AreEqual(1m / 48.3734m, rate);
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_ConvertAsync_ForeignToUah_AppliesBankersRoundingToTwoDecimals()
    {
        StubHttpMessageHandler frankfurter = StubHttpMessageHandler.Json("{}");
        StubHttpMessageHandler privat = StubHttpMessageHandler.Json(_privatBankEurJson);
        CurrencyConverter sut = CreateSut(frankfurter, privat);

        (decimal converted, decimal rate) = await sut.ConvertAsync(100m, "EUR", "UAH", _rateDate);

        Assert.AreEqual(48.3734m, rate);
        // 100 * 48.3734 = 4837.34
        Assert.AreEqual(4837.34m, converted);
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_HardFailure_ThrowsFxUnavailable()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Status(HttpStatusCode.ServiceUnavailable);
        CurrencyConverter sut = CreateSut(handler);

        await Assert.ThrowsExceptionAsync<FxUnavailableException>(
            () => sut.GetRateAsync("EUR", "USD", _rateDate));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_NetworkError_ThrowsFxUnavailable()
    {
        StubHttpMessageHandler handler = StubHttpMessageHandler.Throws(new HttpRequestException("connection refused"));
        CurrencyConverter sut = CreateSut(handler);

        await Assert.ThrowsExceptionAsync<FxUnavailableException>(
            () => sut.GetRateAsync("EUR", "USD", _rateDate));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_MissingTargetCurrency_ThrowsFxUnavailable()
    {
        // Valid JSON but the requested symbol is absent from rates -> never return unconverted.
        StubHttpMessageHandler handler = StubHttpMessageHandler.Json(
            "{\"amount\":1.0,\"base\":\"EUR\",\"date\":\"2024-01-15\",\"rates\":{}}");
        CurrencyConverter sut = CreateSut(handler);

        await Assert.ThrowsExceptionAsync<FxUnavailableException>(
            () => sut.GetRateAsync("EUR", "USD", _rateDate));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_UahPairMissingCurrency_ThrowsFxUnavailable()
    {
        StubHttpMessageHandler frankfurter = StubHttpMessageHandler.Json("{}");
        // PrivatBank payload without the requested currency -> 503, never a silent fallback.
        StubHttpMessageHandler privat = StubHttpMessageHandler.Json(
            "{\"date\":\"15.01.2024\",\"baseCurrencyLit\":\"UAH\",\"exchangeRate\":[]}");
        CurrencyConverter sut = CreateSut(frankfurter, privat);

        await Assert.ThrowsExceptionAsync<FxUnavailableException>(
            () => sut.GetRateAsync("EUR", "UAH", _rateDate));
    }

    private static CurrencyConverter CreateSut(StubHttpMessageHandler frankfurterHandler) =>
        CreateSut(frankfurterHandler, StubHttpMessageHandler.Json("{}"));

    private static CurrencyConverter CreateSut(
        StubHttpMessageHandler frankfurterHandler,
        StubHttpMessageHandler privatBankHandler)
    {
        FrankfurterClient frankfurter = new(new HttpClient(frankfurterHandler)
        {
            BaseAddress = new Uri("https://api.frankfurter.dev"),
        });
        PrivatBankClient privatBank = new(new HttpClient(privatBankHandler)
        {
            BaseAddress = new Uri("https://api.privatbank.ua"),
        });

        MemoryCache cache = new(new MemoryCacheOptions());

        // "Now" well after the rate date so cached past rates persist indefinitely.
        FixedClock clock = new(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        return new CurrencyConverter(frankfurter, privatBank, cache, clock);
    }
}

/// <summary>
/// Hand-rolled <see cref="HttpMessageHandler"/> stub (no Moq): returns a canned response, a status
/// code, or throws a canned exception, and records how many times it was invoked.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage>? _responseFactory;
    private readonly Exception? _exception;

    public int CallCount { get; private set; }

    private StubHttpMessageHandler(Func<HttpResponseMessage>? responseFactory, Exception? exception)
    {
        _responseFactory = responseFactory;
        _exception = exception;
    }

    public static StubHttpMessageHandler Json(string json) => new(
        () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        },
        exception: null);

    public static StubHttpMessageHandler Status(HttpStatusCode statusCode) => new(
        () => new HttpResponseMessage(statusCode),
        exception: null);

    public static StubHttpMessageHandler Throws(Exception exception) => new(
        responseFactory: null,
        exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;

        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(_responseFactory!());
    }
}
