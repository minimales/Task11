namespace task11.ApplicationCore.Models;

public class CreateOperationModel
{

    public Guid TypeId { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string? Note { get; set; }

    public Guid WalletId { get; set; }

    public string? TransactionCurrency { get; set; }
}
