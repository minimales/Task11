using NetArchTest.Rules;
using task11.ApplicationCore.Services;

[TestClass]
public class ArchitectureTests
{
    [TestMethod]
    public void Test_ApplicationCore_DoesNotDependOn_Infrastructure()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(typeof(OperationService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("task11.ApplicationCore")
            .ShouldNot()
            .HaveDependencyOnAny("task11.Infrastructure")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful);
    }

    [TestMethod]
    public void Test_ApplicationCore_DoesNotDependOn_EntityFrameworkCore()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(typeof(OperationService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("task11.ApplicationCore")
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful);
    }

    [TestMethod]
    public void Test_ApplicationCore_DoesNotDependOn_AspNetCoreOrNpgsql()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(typeof(OperationService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("task11.ApplicationCore")
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        Assert.IsTrue(result.IsSuccessful);
    }
}
