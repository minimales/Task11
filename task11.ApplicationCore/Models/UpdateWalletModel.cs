namespace task11.ApplicationCore.Models;

/// <summary>
/// Request body for updating a wallet. The base currency may only be changed
/// while the wallet has no operations; otherwise the service returns 409.
/// </summary>
public sealed class UpdateWalletModel
{
    /// <summary>Display name. Required, 1..100.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ISO-4217 base currency (^[A-Z]{3}$). Immutable once the wallet has operations.
    /// </summary>
    public string BaseCurrency { get; set; } = string.Empty;
}
