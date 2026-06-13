namespace task11.ApplicationCore.Currency;

/// <summary>
/// Configuration for the FX integration, bound from the <c>Fx</c> configuration section.
/// Conversions involving UAH use PrivatBank; all other pairs use Frankfurter (ECB rates).
/// </summary>
public sealed class FxOptions
{
    /// <summary>The configuration section name (<c>Fx</c>).</summary>
    public const string SectionName = "Fx";

    /// <summary>
    /// Base URL of the Frankfurter API (used for non-UAH pairs). Defaults to the canonical host
    /// (<c>https://api.frankfurter.dev</c>); <c>https://api.frankfurter.app</c> is a mirror.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.frankfurter.dev";

    /// <summary>
    /// Base URL of the PrivatBank API (used for any conversion involving UAH, which Frankfurter
    /// does not cover). Defaults to <c>https://api.privatbank.ua</c>.
    /// </summary>
    public string PrivatBankBaseUrl { get; set; } = "https://api.privatbank.ua";

    /// <summary>
    /// Number of retry attempts for transient HTTP failures (exponential backoff).
    /// Defaults to 3.
    /// </summary>
    public int RetryCount { get; set; } = 3;
}
