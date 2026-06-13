namespace task11.ApplicationCore.Currency;

public class FxOptions
{

    public const string SectionName = "Fx";

    public string BaseUrl { get; set; } = "https://api.frankfurter.dev";

    public string PrivatBankBaseUrl { get; set; } = "https://api.privatbank.ua";

    public int RetryCount { get; set; } = 3;
}
