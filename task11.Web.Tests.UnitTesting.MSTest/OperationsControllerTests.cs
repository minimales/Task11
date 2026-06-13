using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.Data.Entities.Enums;
using task11.Web.Controllers;

[TestClass]
public class OperationsControllerTests
{
    private const int _okStatusCode = 200;
    private const int _createdStatusCode = 201;
    private const int _noContentStatusCode = 204;

    private static readonly Guid _walletId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _typeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [TestMethod]
    public async Task Test_OperationsController_GetByWallet_ReturnsOkWithOperations()
    {
        OperationModel[] operations =
        [
            new OperationModel { Id = Guid.NewGuid(), WalletId = _walletId, Amount = 100m, Currency = "UAH" },
            new OperationModel { Id = Guid.NewGuid(), WalletId = _walletId, Amount = 250m, Currency = "UAH" },
        ];
        FakeOperationService operationService = new(byWallet: operations);
        OperationsController controller = new OperationsController(operationService).WithTestContext();

        ActionResult<IReadOnlyList<OperationModel>> result =
            await controller.GetByWallet(_walletId, CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.AreEqual(_okStatusCode, ok.StatusCode);
        IReadOnlyList<OperationModel> body = (IReadOnlyList<OperationModel>)ok.Value!;
        Assert.AreEqual(operations.Length, body.Count);
    }

    [TestMethod]
    public async Task Test_OperationsController_GetById_ReturnsOkWithOperation()
    {
        OperationModel operation = new()
        {
            Id = Guid.NewGuid(),
            WalletId = _walletId,
            TypeId = _typeId,
            Kind = OperationKind.Income,
            Amount = 4050m,
            Currency = "UAH",
        };
        FakeOperationService operationService = new(single: operation);
        OperationsController controller = new OperationsController(operationService).WithTestContext();

        ActionResult<OperationModel> result = await controller.GetById(operation.Id, CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result.Result!;
        OperationModel body = (OperationModel)ok.Value!;
        Assert.AreEqual(operation.Id, body.Id);
        Assert.AreEqual(4050m, body.Amount);
        Assert.AreEqual("UAH", body.Currency);
    }

    [TestMethod]
    public async Task Test_OperationsController_Create_ReturnsCreatedWithConvertedAmountAndAuditNote()
    {
        // Ported from the old xUnit OperationServiceTests.CreateAsync_ForeignCurrency_*:
        // the service converts 100 EUR @ 40.5 -> 4050 UAH and appends an audit note; here we
        // assert the controller surfaces that result as 201 Created.
        OperationModel converted = new()
        {
            Id = Guid.NewGuid(),
            WalletId = _walletId,
            TypeId = _typeId,
            Kind = OperationKind.Income,
            Amount = 4050m,
            Currency = "UAH",
            Note = "Freelance [Original: 100 EUR @ 40.5 on 2024-01-15 → 4050.00 UAH]",
        };
        FakeOperationService operationService = new(single: converted);
        OperationsController controller = new OperationsController(operationService).WithTestContext();

        ActionResult<OperationModel> result = await controller.Create(
            new CreateOperationModel
            {
                WalletId = _walletId,
                TypeId = _typeId,
                Amount = 100m,
                Date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                Note = "Freelance",
                TransactionCurrency = "EUR",
            },
            CancellationToken.None);

        CreatedAtActionResult created = (CreatedAtActionResult)result.Result!;
        Assert.AreEqual(_createdStatusCode, created.StatusCode);
        Assert.AreEqual(nameof(OperationsController.GetById), created.ActionName);
        OperationModel body = (OperationModel)created.Value!;
        Assert.AreEqual(4050m, body.Amount);
        Assert.AreEqual("UAH", body.Currency);
        Assert.IsTrue(body.Note!.StartsWith("Freelance "));
        Assert.IsTrue(body.Note.Contains("100 EUR @ 40.5"));

        // The controller forwarded the original (pre-conversion) request unchanged.
        Assert.IsNotNull(operationService.LastCreateRequest);
        Assert.AreEqual(100m, operationService.LastCreateRequest!.Amount);
        Assert.AreEqual("EUR", operationService.LastCreateRequest.TransactionCurrency);
    }

    [TestMethod]
    public async Task Test_OperationsController_Delete_ReturnsNoContentAndForwardsId()
    {
        FakeOperationService operationService = new();
        OperationsController controller = new OperationsController(operationService).WithTestContext();
        Guid id = Guid.NewGuid();

        IActionResult result = await controller.Delete(id, CancellationToken.None);

        NoContentResult noContent = (NoContentResult)result;
        Assert.AreEqual(_noContentStatusCode, noContent.StatusCode);
        Assert.AreEqual(id, operationService.LastDeletedId);
    }
}
