using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using task11.ApplicationCore;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Time;
using task11.ApplicationCore.Entities;
using static DataTestHelpers;

[TestClass]
public class OperationTypeEntityConfigurationTests
{
    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_MapsToOperationTypesTable()
    {
        using AppDbContext context = RelationalModel();
        Assert.AreEqual("operation_types", EntityType<OperationTypeEntity>(context).GetTableName());
    }

    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_KeyIsId()
    {
        using AppDbContext context = RelationalModel();
        IKey key = EntityType<OperationTypeEntity>(context).FindPrimaryKey()!;
        Assert.AreEqual(1, key.Properties.Count);
        Assert.AreEqual(nameof(OperationTypeEntity.Id), key.Properties[0].Name);
    }

    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_NameIsRequiredWithMaxLength100()
    {
        using AppDbContext context = RelationalModel();
        IProperty name = EntityType<OperationTypeEntity>(context).FindProperty(nameof(OperationTypeEntity.Name))!;
        Assert.IsFalse(name.IsNullable);
        Assert.AreEqual(100, name.GetMaxLength());
    }

    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_DescriptionIsOptionalWithMaxLength500()
    {
        using AppDbContext context = RelationalModel();
        IProperty description = EntityType<OperationTypeEntity>(context).FindProperty(nameof(OperationTypeEntity.Description))!;
        Assert.IsTrue(description.IsNullable);
        Assert.AreEqual(500, description.GetMaxLength());
    }

    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_KindIsRequiredAndStoredAsInt()
    {
        using AppDbContext context = RelationalModel();
        IProperty kind = EntityType<OperationTypeEntity>(context).FindProperty(nameof(OperationTypeEntity.Kind))!;
        Assert.IsFalse(kind.IsNullable);
        Assert.AreEqual(typeof(int), kind.GetProviderClrType());
    }

    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_HasFilteredUniqueIndexOnWalletIdAndName()
    {
        using AppDbContext context = RelationalModel();
        IIndex index = EntityType<OperationTypeEntity>(context).GetIndexes()
            .Single(i => i.Properties.Count == 2);

        Assert.IsTrue(index.IsUnique);
        Assert.AreEqual(nameof(OperationTypeEntity.WalletId), index.Properties[0].Name);
        Assert.AreEqual(nameof(OperationTypeEntity.Name), index.Properties[1].Name);
        Assert.AreEqual("\"IsDeleted\" = false", index.GetFilter());
    }

    [TestMethod]
    public void Test_OperationTypeEntityConfiguration_OperationsForeignKeyUsesRestrict()
    {
        using AppDbContext context = RelationalModel();
        IForeignKey fk = EntityType<FinancialOperationEntity>(context).GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(OperationTypeEntity));

        Assert.AreEqual(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.AreEqual(nameof(FinancialOperationEntity.OperationTypeId), fk.Properties[0].Name);
    }
}
