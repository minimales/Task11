using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.Web.Controllers;

[TestClass]
public class AuthControllerTests
{
    private const string _validUser = "admin";
    private const string _validPassword = "Secret123!";
    private const string _sampleAccessToken = "header.payload.signature";
    private const int _okStatusCode = 200;
    private const int _unauthorizedStatusCode = 401;

    private static AuthController CreateController(FakeAuthService authService)
        => new AuthController(authService, new FakeUserService()).WithTestContext();

    private static AuthTokenModel SampleToken() => new()
    {
        AccessToken = _sampleAccessToken,
        ExpiresAtUtc = new DateTime(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc),
    };

    [TestMethod]
    public async Task Test_AuthController_Login_ValidCredentials_ReturnsOkWithToken()
    {
        AuthTokenModel token = SampleToken();
        FakeAuthService authService = new(_validUser, _validPassword, token);
        AuthController controller = CreateController(authService);

        IActionResult result = await controller.Login(
            new LoginModel { Username = _validUser, Password = _validPassword },
            CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result;
        Assert.AreEqual(_okStatusCode, ok.StatusCode);
        AuthTokenModel body = (AuthTokenModel)ok.Value!;
        Assert.AreEqual(_sampleAccessToken, body.AccessToken);
        Assert.AreEqual(token.ExpiresAtUtc, body.ExpiresAtUtc);
    }

    [DataTestMethod]
    [DataRow("admin", "wrong-password")]
    [DataRow("nobody", "Secret123!")]
    public async Task Test_AuthController_Login_InvalidCredentials_ReturnsUnauthorized(
        string username,
        string password)
    {
        FakeAuthService authService = new(_validUser, _validPassword, SampleToken());
        AuthController controller = CreateController(authService);

        IActionResult result = await controller.Login(
            new LoginModel { Username = username, Password = password },
            CancellationToken.None);

        UnauthorizedResult unauthorized = (UnauthorizedResult)result;
        Assert.AreEqual(_unauthorizedStatusCode, unauthorized.StatusCode);
    }

    [TestMethod]
    public async Task Test_AuthController_Login_ValidCredentials_PassesRequestToService()
    {
        FakeAuthService authService = new(_validUser, _validPassword, SampleToken());
        AuthController controller = CreateController(authService);

        await controller.Login(
            new LoginModel { Username = _validUser, Password = _validPassword },
            CancellationToken.None);

        Assert.IsNotNull(authService.LastRequest);
        Assert.AreEqual(_validUser, authService.LastRequest!.Username);
    }

    [TestMethod]
    public async Task Test_AuthController_GetUsers_ReturnsOkWithUsers()
    {
        UserModel[] users =
        [
            new UserModel { Id = Guid.NewGuid(), Username = "alice", Role = "Admin" },
            new UserModel { Id = Guid.NewGuid(), Username = "bob", Role = "User" },
        ];
        AuthController controller = new AuthController(
            new FakeAuthService(_validUser, _validPassword, SampleToken()),
            new FakeUserService(users)).WithTestContext();

        IActionResult result = await controller.GetUsers(CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result;
        Assert.AreEqual(_okStatusCode, ok.StatusCode);
        IReadOnlyList<UserModel> body = (IReadOnlyList<UserModel>)ok.Value!;
        Assert.AreEqual(users.Length, body.Count);
    }
}
