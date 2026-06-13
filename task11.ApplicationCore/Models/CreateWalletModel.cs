namespace task11.ApplicationCore.Models;

/// <summary>
/// Request body for creating a personal wallet. The wallet is owned by the
/// authenticated caller; <see cref="BaseCurrency"/> defaults to "UAH" when omitted.
/// </summary>
public sealed class CreateWalletModel
{
    /// <summary>Display name. Required, 1..100.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ISO-4217 base currency (^[A-Z]{3}$). Defaults to "UAH" when null/empty.</summary>
    public string? BaseCurrency { get; set; }
}
