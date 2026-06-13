namespace task11.Data.Entities.Enums;

/// <summary>
/// Direction of a financial operation. Stored as <see cref="int"/>.
/// Drives report totals (income vs expense).
/// </summary>
public enum OperationKind
{
    Income = 1,
    Expense = 2
}
