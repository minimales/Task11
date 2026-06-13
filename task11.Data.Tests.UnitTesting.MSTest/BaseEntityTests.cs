using task11.Data.Entities;

[TestClass]
public class BaseEntityTests
{
    [TestMethod]
    public void Test_BaseEntity_Id_DefaultsToNonEmptyGuid()
    {
        WalletEntity entity = new();
        Assert.AreNotEqual(Guid.Empty, entity.Id);
    }

    [TestMethod]
    public void Test_BaseEntity_NewInstances_HaveDistinctIds()
    {
        WalletEntity first = new();
        WalletEntity second = new();
        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestMethod]
    public void Test_BaseEntity_IsDeleted_DefaultsToFalse()
    {
        UserEntity entity = new();
        Assert.IsFalse(entity.IsDeleted);
        Assert.IsNull(entity.DeletedAtUtc);
        Assert.IsNull(entity.UpdatedAtUtc);
    }
}
