using task11.ApplicationCore.Entities.Enums;

namespace task11.ApplicationCore.Models;

public class CreateOperationTypeModel
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public OperationKind Kind { get; set; }
}
