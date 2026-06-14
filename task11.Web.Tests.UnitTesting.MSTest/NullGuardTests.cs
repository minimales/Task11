using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;
using task11.Web.Controllers;
using task11.Web.Infrastructure;
using task11.Web.Infrastructure.Auth;
using task11.Web.Middleware;

[TestClass]
public class NullGuardTests
{
    private static RequestDelegate ValidNext => static (HttpContext _) => Task.CompletedTask;

    private static IConfiguration ValidConfiguration => new ConfigurationBuilder().Build();

    // ----- AuthController -----

    [TestMethod]
    public void Test_AuthController_Constructor_NullAuthService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuthController(null!, new FakeUserService()));
    }

    [TestMethod]
    public void Test_AuthController_Constructor_NullUserService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuthController(new FakeAuthService("u", "p", new AuthTokenModel()), null!));
    }

    // ----- WalletsController -----

    [TestMethod]
    public void Test_WalletsController_Constructor_NullWalletService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new WalletsController(null!));
    }

    // ----- OperationTypesController -----

    [TestMethod]
    public void Test_OperationTypesController_Constructor_NullService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationTypesController(null!));
    }

    // ----- OperationsController -----

    [TestMethod]
    public void Test_OperationsController_Constructor_NullOperations_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationsController(null!));
    }

    // ----- ReportsController -----

    [TestMethod]
    public void Test_ReportsController_Constructor_NullReportService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new ReportsController(null!));
    }

    // ----- CorrelationIdMiddleware -----

    [TestMethod]
    public void Test_CorrelationIdMiddleware_Constructor_NullNext_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new CorrelationIdMiddleware(null!));
    }

    // ----- RequestResponseLoggingMiddleware -----

    [TestMethod]
    public void Test_RequestResponseLoggingMiddleware_Constructor_NullNext_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new RequestResponseLoggingMiddleware(
                null!,
                NullLogger<RequestResponseLoggingMiddleware>.Instance,
                ValidConfiguration));
    }

    [TestMethod]
    public void Test_RequestResponseLoggingMiddleware_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new RequestResponseLoggingMiddleware(
                ValidNext,
                null!,
                ValidConfiguration));
    }

    [TestMethod]
    public void Test_RequestResponseLoggingMiddleware_Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new RequestResponseLoggingMiddleware(
                ValidNext,
                NullLogger<RequestResponseLoggingMiddleware>.Instance,
                null!));
    }

    // ----- CurrentUser -----

    [TestMethod]
    public void Test_CurrentUser_Constructor_NullHttpContextAccessor_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new CurrentUser(null!));
    }

    // ----- GlobalExceptionHandler -----

    [TestMethod]
    public void Test_GlobalExceptionHandler_Constructor_NullProblemDetailsService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new GlobalExceptionHandler(
                null!,
                NullLogger<GlobalExceptionHandler>.Instance));
    }

    [TestMethod]
    public void Test_GlobalExceptionHandler_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new GlobalExceptionHandler(
                new FakeProblemDetailsService(),
                null!));
    }

    // ----- ProblemDetailsEnricher -----

    [TestMethod]
    public void Test_ProblemDetailsEnricher_Enrich_NullProblemDetails_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => ProblemDetailsEnricher.Enrich(new DefaultHttpContext(), null!));
    }

    // ----- ValidationProblemResultFactory -----

    [TestMethod]
    public void Test_ValidationProblemResultFactory_CreateActionResult_NullContext_ThrowsArgumentNullException()
    {
        ValidationProblemResultFactory factory = new();

        Assert.ThrowsException<ArgumentNullException>(
            () => factory.CreateActionResult(null!, new ValidationProblemDetails()));
    }
}

internal class FakeOperationTypeService : IOperationTypeService
{
    public Task<IReadOnlyList<OperationTypeModel>> GetByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OperationTypeModel>>(Array.Empty<OperationTypeModel>());

    public Task<OperationTypeModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationTypeModel { Id = id });

    public Task<OperationTypeModel> CreateAsync(
        Guid walletId,
        CreateOperationTypeModel request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationTypeModel { Id = Guid.NewGuid() });

    public Task<OperationTypeModel> UpdateAsync(
        Guid id,
        UpdateOperationTypeModel request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationTypeModel { Id = id });

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class FakeProblemDetailsService : IProblemDetailsService
{
    public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;

    public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context) => ValueTask.FromResult(true);
}
