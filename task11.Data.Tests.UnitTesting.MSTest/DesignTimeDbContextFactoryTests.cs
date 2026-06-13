using task11.Data;

[TestClass]
public class DesignTimeDbContextFactoryTests
{
    [TestMethod]
    public void Test_DesignTimeDbContextFactory_ImplementsDesignTimeFactoryInterface()
    {
        bool implements = typeof(DesignTimeDbContextFactory).GetInterfaces()
            .Any(i => i.Name.StartsWith("IDesignTimeDbContextFactory", StringComparison.Ordinal));
        Assert.IsTrue(implements);
    }

    [TestMethod]
    public void Test_DesignTimeDbContextFactory_CreateDbContext_ReturnsNpgsqlContext()
    {
        DesignTimeDbContextFactory factory = new();
        using AppDbContext context = factory.CreateDbContext(Array.Empty<string>());

        Assert.IsNotNull(context);
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }
}
