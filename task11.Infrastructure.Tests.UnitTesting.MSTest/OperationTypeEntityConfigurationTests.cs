using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using task11.Infrastructure.Persistence;
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

    [DataTestMethod]
    [DataRow(nameof(OperationTypeEntity.Name), false, 100)]
    [DataRow(nameof(OperationTypeEntity.Description), true, 500)]
    public void Test_OperationTypeEntityConfiguration_PropertyHasExpectedNullabilityAndMaxLength(
        string propertyName, bool expectedNullable, int expectedMaxLength)
    {
        using AppDbContext context = RelationalModel();
        IProperty property = EntityType<OperationTypeEntity>(context).FindProperty(propertyName)!;
        Assert.AreEqual(expectedNullable, property.IsNullable);
        Assert.AreEqual(expectedMaxLength, property.GetMaxLength());
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
