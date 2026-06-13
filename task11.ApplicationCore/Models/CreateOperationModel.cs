namespace task11.ApplicationCore.Models;

/// <summary>
/// Request to create a financial operation. The amount is provided in
/// <see cref="TransactionCurrency"/> (or the wallet base currency when omitted) and is
/// converted to the wallet base currency on the server before being stored.
/// </summary>
public sealed class CreateOperationModel
{
    /// <summary>The operation type id (carries the income/expense kind). FK validated in the service.</summary>
    public Guid TypeId { get; set; }

    /// <summary>The original transaction amount. Must be &gt; 0.</summary>
    public decimal Amount { get; set; }

    /// <summary>The operation date. Converted to UTC at the boundary.</summary>
    public DateTime Date { get; set; }

    /// <summary>Optional free-text note (&lt;= 500 chars). A conversion-audit string may be appended.</summary>
    public string? Note { get; set; }

    /// <summary>The target wallet id. Ownership is checked in the service.</summary>
    public Guid WalletId { get; set; }

    /// <summary>
    /// Optional ISO-4217 currency of <see cref="Amount"/>. When null or equal to the wallet base
    /// currency the amount is stored as-is; otherwise it is converted at the historical rate for <see cref="Date"/>.
    /// </summary>
    public string? TransactionCurrency { get; set; }
}
