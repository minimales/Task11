namespace task11.ApplicationCore.Models;

public class UpdateOperationModel
{

    public Guid TypeId { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string? Note { get; set; }

    public string? TransactionCurrency { get; set; }
}
