using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.Web.Controllers;

[TestClass]
public class WalletsControllerTests
{
    private const int _okStatusCode = 200;
    private const int _createdStatusCode = 201;
    private const int _noContentStatusCode = 204;

    [TestMethod]
    public async Task Test_WalletsController_GetAll_ReturnsOkWithAccessibleWallets()
    {
        WalletModel[] wallets =
        [
            new WalletModel { Id = Guid.NewGuid(), Name = "Personal", BaseCurrency = "UAH" },
            new WalletModel { Id = Guid.NewGuid(), Name = "Shared", BaseCurrency = "USD" },
        ];
        FakeWalletService walletService = new(accessible: wallets);
        WalletsController controller = new WalletsController(walletService).WithTestContext();

        ActionResult<IReadOnlyList<WalletModel>> result = await controller.GetAll(CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.AreEqual(_okStatusCode, ok.StatusCode);
        IReadOnlyList<WalletModel> body = (IReadOnlyList<WalletModel>)ok.Value!;
        Assert.AreEqual(wallets.Length, body.Count);
    }

    [TestMethod]
    public async Task Test_WalletsController_GetById_ReturnsOkWithWallet()
    {
        WalletModel wallet = new() { Id = Guid.NewGuid(), Name = "Travel", BaseCurrency = "EUR" };
        FakeWalletService walletService = new(single: wallet);
        WalletsController controller = new WalletsController(walletService).WithTestContext();

        ActionResult<WalletModel> result = await controller.GetById(wallet.Id, CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result.Result!;
        WalletModel body = (WalletModel)ok.Value!;
        Assert.AreEqual(wallet.Id, body.Id);
        Assert.AreEqual("Travel", body.Name);
        Assert.AreEqual("EUR", body.BaseCurrency);
    }

    [TestMethod]
    public async Task Test_WalletsController_Create_ReturnsCreatedAtActionWithWallet()
    {
        WalletModel wallet = new() { Id = Guid.NewGuid() };
        FakeWalletService walletService = new(single: wallet);
        WalletsController controller = new WalletsController(walletService).WithTestContext();

        ActionResult<WalletModel> result = await controller.Create(
            new CreateWalletModel { Name = "Savings", BaseCurrency = "GBP" },
            CancellationToken.None);

        CreatedAtActionResult created = (CreatedAtActionResult)result.Result!;
        Assert.AreEqual(_createdStatusCode, created.StatusCode);
        Assert.AreEqual(nameof(WalletsController.GetById), created.ActionName);
        WalletModel body = (WalletModel)created.Value!;
        Assert.AreEqual("Savings", body.Name);
        Assert.AreEqual("GBP", body.BaseCurrency);
    }

    [TestMethod]
    public async Task Test_WalletsController_Create_NullCurrency_DefaultsToUah()
    {
        FakeWalletService walletService = new();
        WalletsController controller = new WalletsController(walletService).WithTestContext();

        ActionResult<WalletModel> result = await controller.Create(
            new CreateWalletModel { Name = "Default", BaseCurrency = null },
            CancellationToken.None);

        CreatedAtActionResult created = (CreatedAtActionResult)result.Result!;
        WalletModel body = (WalletModel)created.Value!;
        Assert.AreEqual("UAH", body.BaseCurrency);
    }

    [TestMethod]
    public async Task Test_WalletsController_Delete_ReturnsNoContent()
    {
        FakeWalletService walletService = new();
        WalletsController controller = new WalletsController(walletService).WithTestContext();

        IActionResult result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        NoContentResult noContent = (NoContentResult)result;
        Assert.AreEqual(_noContentStatusCode, noContent.StatusCode);
    }
}
