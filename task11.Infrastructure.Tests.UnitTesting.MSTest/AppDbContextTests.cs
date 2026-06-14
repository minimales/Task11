using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Time;
using task11.ApplicationCore.Entities;
using task11.ApplicationCore.Entities.Enums;
using static DataTestHelpers;

[TestClass]
public class AppDbContextTests
{
    [TestMethod]
    public void Test_AppDbContext_Constructor_NullConnectionString_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AppDbContext(null!, useInMemory: true, new SystemClock()));
    }

    [TestMethod]
    public void Test_AppDbContext_Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AppDbContext("db", useInMemory: true, null!));
    }

    [TestMethod]
    public void Test_AppDbContext_DbSets_AreNotNull()
    {
        using AppDbContext context = InMemory(nameof(Test_AppDbContext_DbSets_AreNotNull));
        Assert.IsNotNull(context.Users);
        Assert.IsNotNull(context.Wallets);
        Assert.IsNotNull(context.OperationTypes);
        Assert.IsNotNull(context.FinancialOperations);
    }

    [TestMethod]
    public void Test_AppDbContext_UseInMemoryDatabase_ProviderIsInMemory()
    {
        using AppDbContext context = InMemory(nameof(Test_AppDbContext_UseInMemoryDatabase_ProviderIsInMemory));
        Assert.AreEqual("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }

    [TestMethod]
    public void Test_AppDbContext_UseNpgsqlProvider_ProviderIsNpgsql()
    {
        using AppDbContext context = RelationalModel();
        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [TestMethod]
    public void Test_AppDbContext_InMemory_SaveAndRetrieveUserEntity()
    {
        using AppDbContext context = InMemory(nameof(Test_AppDbContext_InMemory_SaveAndRetrieveUserEntity));
        UserEntity user = new() { Username = "alice", PasswordHash = "hash", Role = "Admin" };
        context.Users.Add(user);
        context.SaveChanges();

        UserEntity? retrieved = context.Users.Find(user.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("alice", retrieved.Username);
        Assert.AreEqual("Admin", retrieved.Role);
    }

    [TestMethod]
    public void Test_AppDbContext_InMemory_SaveAndRetrieveWalletEntity()
    {
        using AppDbContext context = InMemory(nameof(Test_AppDbContext_InMemory_SaveAndRetrieveWalletEntity));
        WalletEntity wallet = new() { Name = "Shared", BaseCurrency = "USD", OwnerUserId = null };
        context.Wallets.Add(wallet);
        context.SaveChanges();

        WalletEntity? retrieved = context.Wallets.Find(wallet.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Shared", retrieved.Name);
        Assert.AreEqual("USD", retrieved.BaseCurrency);
        Assert.IsNull(retrieved.OwnerUserId);
    }

    [TestMethod]
    public void Test_AppDbContext_InMemory_SaveAndRetrieveFinancialOperationEntity()
    {
        using AppDbContext context = InMemory(nameof(Test_AppDbContext_InMemory_SaveAndRetrieveFinancialOperationEntity));
        WalletEntity wallet = new() { Name = "W", BaseCurrency = "UAH" };
        OperationTypeEntity type = new() { Name = "Salary", Kind = OperationKind.Income, WalletId = wallet.Id };
        FinancialOperationEntity op = new()
        {
            WalletId = wallet.Id,
            OperationTypeId = type.Id,
            Amount = 1234.56m,
            OccurredAtUtc = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Note = "Freelance"
        };
        context.Wallets.Add(wallet);
        context.OperationTypes.Add(type);
        context.FinancialOperations.Add(op);
        context.SaveChanges();

        FinancialOperationEntity? retrieved = context.FinancialOperations.Find(op.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(1234.56m, retrieved.Amount);
        Assert.AreEqual("Freelance", retrieved.Note);
        Assert.AreEqual(wallet.Id, retrieved.WalletId);
    }

    [TestMethod]
    public void Test_AppDbContext_InMemory_WalletOperationsNavigation_LoadsRelatedOperations()
    {
        string db = nameof(Test_AppDbContext_InMemory_WalletOperationsNavigation_LoadsRelatedOperations);
        Guid walletId;
        using (AppDbContext context = InMemory(db))
        {
            WalletEntity wallet = new() { Name = "W", BaseCurrency = "UAH" };
            OperationTypeEntity type = new() { Name = "Salary", Kind = OperationKind.Income, WalletId = wallet.Id };
            context.Wallets.Add(wallet);
            context.OperationTypes.Add(type);
            context.FinancialOperations.Add(new FinancialOperationEntity
            {
                WalletId = wallet.Id,
                OperationTypeId = type.Id,
                Amount = 10m,
                OccurredAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            context.FinancialOperations.Add(new FinancialOperationEntity
            {
                WalletId = wallet.Id,
                OperationTypeId = type.Id,
                Amount = 20m,
                OccurredAtUtc = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            });
            context.SaveChanges();
            walletId = wallet.Id;
        }

        using AppDbContext read = InMemory(db);
        WalletEntity loaded = read.Wallets.Include(w => w.Operations).Single(w => w.Id == walletId);
        Assert.IsNotNull(loaded.Operations);
        Assert.AreEqual(2, loaded.Operations.Count);
    }

    [TestMethod]
    public void Test_AppDbContext_InMemory_UserOwnedWalletsNavigation_LoadsRelatedWallets()
    {
        string db = nameof(Test_AppDbContext_InMemory_UserOwnedWalletsNavigation_LoadsRelatedWallets);
        Guid userId;
        using (AppDbContext context = InMemory(db))
        {
            UserEntity user = new() { Username = "bob", PasswordHash = "h" };
            context.Users.Add(user);
            context.Wallets.Add(new WalletEntity { Name = "Personal A", BaseCurrency = "UAH", OwnerUserId = user.Id });
            context.Wallets.Add(new WalletEntity { Name = "Personal B", BaseCurrency = "UAH", OwnerUserId = user.Id });
            context.SaveChanges();
            userId = user.Id;
        }

        using AppDbContext read = InMemory(db);
        UserEntity loaded = read.Users.Include(u => u.OwnedWallets).Single(u => u.Id == userId);
        Assert.IsNotNull(loaded.OwnedWallets);
        Assert.AreEqual(2, loaded.OwnedWallets.Count);
    }

    [TestMethod]
    public void Test_AppDbContext_SoftDeleteFilter_HidesDeletedRowsFromSubsequentReads()
    {
        string db = nameof(Test_AppDbContext_SoftDeleteFilter_HidesDeletedRowsFromSubsequentReads);
        Guid walletId;
        using (AppDbContext context = InMemory(db))
        {
            WalletEntity wallet = new() { Name = "ToDelete", BaseCurrency = "UAH" };
            context.Wallets.Add(wallet);
            context.SaveChanges();
            walletId = wallet.Id;

            context.Wallets.Remove(wallet);
            context.SaveChanges();
        }

        using AppDbContext read = InMemory(db);
        Assert.AreEqual(0, read.Wallets.Count());
        Assert.IsNull(read.Wallets.FirstOrDefault(w => w.Id == walletId));
    }

    [TestMethod]
    public void Test_AppDbContext_SoftDeleteFilter_IgnoreQueryFilters_RevealsDeletedRows()
    {
        string db = nameof(Test_AppDbContext_SoftDeleteFilter_IgnoreQueryFilters_RevealsDeletedRows);
        Guid walletId;
        using (AppDbContext context = InMemory(db))
        {
            WalletEntity wallet = new() { Name = "ToDelete", BaseCurrency = "UAH" };
            context.Wallets.Add(wallet);
            context.SaveChanges();
            walletId = wallet.Id;

            context.Wallets.Remove(wallet);
            context.SaveChanges();
        }

        using AppDbContext read = InMemory(db);
        WalletEntity? hidden = read.Wallets.IgnoreQueryFilters().FirstOrDefault(w => w.Id == walletId);
        Assert.IsNotNull(hidden);
        Assert.IsTrue(hidden.IsDeleted);
        Assert.IsNotNull(hidden.DeletedAtUtc);
    }

    [TestMethod]
    public void Test_AppDbContext_SoftDeleteInterceptor_StampsDeletedAtUtcFromClock()
    {
        string db = nameof(Test_AppDbContext_SoftDeleteInterceptor_StampsDeletedAtUtcFromClock);
        FixedClock clock = FixedClock.At(2024, 6, 1);
        Guid walletId;
        using (AppDbContext context = InMemory(db, clock))
        {
            WalletEntity wallet = new() { Name = "W", BaseCurrency = "UAH" };
            context.Wallets.Add(wallet);
            context.SaveChanges();
            walletId = wallet.Id;

            clock.UtcNow = new DateTime(2025, 9, 9, 0, 0, 0, DateTimeKind.Utc);
            context.Wallets.Remove(wallet);
            context.SaveChanges();
        }

        using AppDbContext read = InMemory(db);
        WalletEntity deleted = read.Wallets.IgnoreQueryFilters().Single(w => w.Id == walletId);
        Assert.AreEqual(new DateTime(2025, 9, 9, 0, 0, 0, DateTimeKind.Utc), deleted.DeletedAtUtc);
    }

    [TestMethod]
    public void Test_AppDbContext_AuditInterceptor_StampsCreatedAtUtcOnInsert()
    {
        string db = nameof(Test_AppDbContext_AuditInterceptor_StampsCreatedAtUtcOnInsert);
        FixedClock clock = FixedClock.At(2024, 2, 2);
        Guid userId;
        using (AppDbContext context = InMemory(db, clock))
        {
            UserEntity user = new() { Username = "carol", PasswordHash = "h" };
            context.Users.Add(user);
            context.SaveChanges();
            userId = user.Id;
        }

        using AppDbContext read = InMemory(db);
        UserEntity stored = read.Users.Single(u => u.Id == userId);
        Assert.AreEqual(new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc), stored.CreatedAtUtc);
        Assert.IsNull(stored.UpdatedAtUtc);
    }

    [TestMethod]
    public void Test_AppDbContext_AuditInterceptor_StampsUpdatedAtUtcOnUpdate()
    {
        string db = nameof(Test_AppDbContext_AuditInterceptor_StampsUpdatedAtUtcOnUpdate);
        FixedClock clock = FixedClock.At(2024, 2, 2);
        Guid userId;
        using (AppDbContext context = InMemory(db, clock))
        {
            UserEntity user = new() { Username = "dave", PasswordHash = "h" };
            context.Users.Add(user);
            context.SaveChanges();
            userId = user.Id;

            clock.UtcNow = new DateTime(2024, 3, 3, 0, 0, 0, DateTimeKind.Utc);
            user.Role = "Admin";
            context.SaveChanges();
        }

        using AppDbContext read = InMemory(db);
        UserEntity stored = read.Users.Single(u => u.Id == userId);
        Assert.AreEqual(new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc), stored.CreatedAtUtc);
        Assert.AreEqual(new DateTime(2024, 3, 3, 0, 0, 0, DateTimeKind.Utc), stored.UpdatedAtUtc);
    }

    [TestMethod]
    public void Test_AppDbContext_Model_ContainsExactlyFourEntityTypes()
    {
        using AppDbContext context = RelationalModel();
        Assert.AreEqual(4, context.Model.GetEntityTypes().Count());
    }

    [TestMethod]
    public void Test_AppDbContext_Model_ContainsExpectedEntityTypes()
    {
        using AppDbContext context = RelationalModel();
        string[] names = context.Model.GetEntityTypes()
            .Select(t => t.ClrType.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(FinancialOperationEntity),
                nameof(OperationTypeEntity),
                nameof(UserEntity),
                nameof(WalletEntity)
            },
            names);
    }

    [TestMethod]
    public void Test_AppDbContext_Model_AllEntitiesHaveSoftDeleteQueryFilter()
    {
        using AppDbContext context = RelationalModel();
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            Assert.IsNotNull(
                entityType.GetQueryFilter(),
                $"{entityType.ClrType.Name} should have a soft-delete query filter.");
        }
    }
}
