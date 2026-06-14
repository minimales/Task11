using NetArchTest.Rules;
using task11.ApplicationCore.Services;

[TestClass]
public class ArchitectureTests
{
    [DataTestMethod]
    [DataRow("task11.Infrastructure")]
    [DataRow("Microsoft.EntityFrameworkCore")]
    [DataRow("Microsoft.AspNetCore,Npgsql")]
    public void Test_ApplicationCore_DoesNotDependOn_ForbiddenDependencies(string forbiddenNamespaces)
    {
        string[] dependencies = forbiddenNamespaces.Split(',');

        NetArchTest.Rules.TestResult result = Types.InAssembly(typeof(OperationService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("task11.ApplicationCore")
            .ShouldNot()
            .HaveDependencyOnAny(dependencies)
            .GetResult();

        Assert.IsTrue(result.IsSuccessful);
    }
}
