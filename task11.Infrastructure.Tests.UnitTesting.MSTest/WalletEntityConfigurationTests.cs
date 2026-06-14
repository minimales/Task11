using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using task11.Infrastructure.Persistence;
using task11.ApplicationCore.Entities;
using static DataTestHelpers;

[TestClass]
public class WalletEntityConfigurationTests
{
    [TestMethod]
    public void Test_WalletEntityConfiguration_MapsToWalletsTable()
    {
        using AppDbContext context = RelationalModel();
        Assert.AreEqual("wallets", EntityType<WalletEntity>(context).GetTableName());
    }

    [TestMethod]
    public void Test_WalletEntityConfiguration_KeyIsId()
    {
        using AppDbContext context = RelationalModel();
        IKey key = EntityType<WalletEntity>(context).FindPrimaryKey()!;
        Assert.AreEqual(1, key.Properties.Count);
        Assert.AreEqual(nameof(WalletEntity.Id), key.Properties[0].Name);
    }

    [TestMethod]
    public void Test_WalletEntityConfiguration_NameIsRequiredWithMaxLength100()
    {
        using AppDbContext context = RelationalModel();
        IProperty name = EntityType<WalletEntity>(context).FindProperty(nameof(WalletEntity.Name))!;
        Assert.IsFalse(name.IsNullable);
        Assert.AreEqual(100, name.GetMaxLength());
    }

    [TestMethod]
    public void Test_WalletEntityConfiguration_BaseCurrencyIsFixedLengthChar3WithDefaultUAH()
    {
        using AppDbContext context = RelationalModel();
        IProperty currency = EntityType<WalletEntity>(context).FindProperty(nameof(WalletEntity.BaseCurrency))!;
        Assert.IsFalse(currency.IsNullable);
        Assert.AreEqual(3, currency.GetMaxLength());
        Assert.IsTrue(currency.IsFixedLength());
        Assert.AreEqual("char(3)", currency.GetColumnType());
        Assert.AreEqual("UAH", currency.GetDefaultValue());
    }

    [TestMethod]
    public void Test_WalletEntityConfiguration_OwnerUserIdIsNullable()
    {
        using AppDbContext context = RelationalModel();
        IProperty owner = EntityType<WalletEntity>(context).FindProperty(nameof(WalletEntity.OwnerUserId))!;
        Assert.IsTrue(owner.IsNullable);
    }

    [TestMethod]
    public void Test_WalletEntityConfiguration_HasIndexOnOwnerUserId()
    {
        using AppDbContext context = RelationalModel();
        IIndex index = EntityType<WalletEntity>(context).GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(WalletEntity.OwnerUserId));

        Assert.IsFalse(index.IsUnique);
    }

    [DataTestMethod]
    [DataRow(typeof(FinancialOperationEntity), "WalletId")]
    [DataRow(typeof(OperationTypeEntity), "WalletId")]
    public void Test_WalletEntityConfiguration_ChildForeignKeyToWalletUsesRestrict(
        Type declaringEntityType, string expectedForeignKeyProperty)
    {
        using AppDbContext context = RelationalModel();
        IForeignKey fk = context.Model.FindEntityType(declaringEntityType)!.GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(WalletEntity));

        Assert.AreEqual(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.AreEqual(expectedForeignKeyProperty, fk.Properties[0].Name);
    }
}
