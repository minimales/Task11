using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using task11.ApplicationCore;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Time;
using task11.ApplicationCore.Entities;
using static DataTestHelpers;

[TestClass]
public class FinancialOperationEntityConfigurationTests
{
    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_MapsToFinancialOperationsTable()
    {
        using AppDbContext context = RelationalModel();
        Assert.AreEqual("financial_operations", EntityType<FinancialOperationEntity>(context).GetTableName());
    }

    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_KeyIsId()
    {
        using AppDbContext context = RelationalModel();
        IKey key = EntityType<FinancialOperationEntity>(context).FindPrimaryKey()!;
        Assert.AreEqual(1, key.Properties.Count);
        Assert.AreEqual(nameof(FinancialOperationEntity.Id), key.Properties[0].Name);
    }

    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_AmountHasPrecision18Scale2()
    {
        using AppDbContext context = RelationalModel();
        IProperty amount = EntityType<FinancialOperationEntity>(context).FindProperty(nameof(FinancialOperationEntity.Amount))!;
        Assert.AreEqual(18, amount.GetPrecision());
        Assert.AreEqual(2, amount.GetScale());
    }

    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_OccurredAtUtcIsRequired()
    {
        using AppDbContext context = RelationalModel();
        IProperty occurred = EntityType<FinancialOperationEntity>(context).FindProperty(nameof(FinancialOperationEntity.OccurredAtUtc))!;
        Assert.IsFalse(occurred.IsNullable);
    }

    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_NoteIsOptionalWithMaxLength1000()
    {
        using AppDbContext context = RelationalModel();
        IProperty note = EntityType<FinancialOperationEntity>(context).FindProperty(nameof(FinancialOperationEntity.Note))!;
        Assert.IsTrue(note.IsNullable);
        Assert.AreEqual(1000, note.GetMaxLength());
    }

    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_HasCompositeIndexOnWalletIdAndOccurredAtUtc()
    {
        using AppDbContext context = RelationalModel();
        IIndex index = EntityType<FinancialOperationEntity>(context).GetIndexes()
            .Single(i => i.Properties.Count == 2);

        Assert.IsFalse(index.IsUnique);
        Assert.AreEqual(nameof(FinancialOperationEntity.WalletId), index.Properties[0].Name);
        Assert.AreEqual(nameof(FinancialOperationEntity.OccurredAtUtc), index.Properties[1].Name);
    }

    [TestMethod]
    public void Test_FinancialOperationEntityConfiguration_HasIndexOnOperationTypeId()
    {
        using AppDbContext context = RelationalModel();
        IIndex index = EntityType<FinancialOperationEntity>(context).GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(FinancialOperationEntity.OperationTypeId));

        Assert.IsFalse(index.IsUnique);
    }
}
