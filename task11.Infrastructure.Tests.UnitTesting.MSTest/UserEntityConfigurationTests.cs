using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using task11.ApplicationCore;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Time;
using task11.ApplicationCore.Entities;
using static DataTestHelpers;

[TestClass]
public class UserEntityConfigurationTests
{
    [TestMethod]
    public void Test_UserEntityConfiguration_MapsToUsersTable()
    {
        using AppDbContext context = RelationalModel();
        Assert.AreEqual("users", EntityType<UserEntity>(context).GetTableName());
    }

    [TestMethod]
    public void Test_UserEntityConfiguration_KeyIsId()
    {
        using AppDbContext context = RelationalModel();
        IKey key = EntityType<UserEntity>(context).FindPrimaryKey()!;
        Assert.AreEqual(1, key.Properties.Count);
        Assert.AreEqual(nameof(UserEntity.Id), key.Properties[0].Name);
    }

    [TestMethod]
    public void Test_UserEntityConfiguration_UsernameIsRequiredWithMaxLength50()
    {
        using AppDbContext context = RelationalModel();
        IProperty username = EntityType<UserEntity>(context).FindProperty(nameof(UserEntity.Username))!;
        Assert.IsFalse(username.IsNullable);
        Assert.AreEqual(50, username.GetMaxLength());
    }

    [TestMethod]
    public void Test_UserEntityConfiguration_PasswordHashIsRequiredWithMaxLength512()
    {
        using AppDbContext context = RelationalModel();
        IProperty hash = EntityType<UserEntity>(context).FindProperty(nameof(UserEntity.PasswordHash))!;
        Assert.IsFalse(hash.IsNullable);
        Assert.AreEqual(512, hash.GetMaxLength());
    }

    [TestMethod]
    public void Test_UserEntityConfiguration_RoleIsRequiredWithMaxLength20AndDefaultUser()
    {
        using AppDbContext context = RelationalModel();
        IProperty role = EntityType<UserEntity>(context).FindProperty(nameof(UserEntity.Role))!;
        Assert.IsFalse(role.IsNullable);
        Assert.AreEqual(20, role.GetMaxLength());
        Assert.AreEqual("User", role.GetDefaultValue());
    }

    [TestMethod]
    public void Test_UserEntityConfiguration_HasFilteredUniqueIndexOnUsername()
    {
        using AppDbContext context = RelationalModel();
        IIndex index = EntityType<UserEntity>(context).GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(UserEntity.Username));

        Assert.IsTrue(index.IsUnique);
        Assert.AreEqual("\"IsDeleted\" = false", index.GetFilter());
    }

    [TestMethod]
    public void Test_UserEntityConfiguration_OwnedWalletsForeignKeyUsesRestrict()
    {
        using AppDbContext context = RelationalModel();
        IForeignKey fk = EntityType<WalletEntity>(context).GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(UserEntity));

        Assert.AreEqual(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.AreEqual(nameof(WalletEntity.OwnerUserId), fk.Properties[0].Name);
    }
}
