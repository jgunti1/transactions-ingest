using System.Transactions;
using Microsoft.EntityFrameWorkCore;
using TransactionsIngest.Data;
using TransactionsIngest.Models;
using TransactionsIngest.Services;

namespace TransactionsIngest.Tests;

public class IngestionServiceTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Build();

        return new AppDbContext(options);
    }

    [Fact]
    public async Task NewTransaction_IsInserted()
    {
        // arrange
        var db = CreateInMemoryDb();
        var mockService = new MockTransactionService();
        var ingestionService = new IngestionService(db, mockService);

        // act
        await ingestionService.RunAsync();

        // assert
        var count = await db.Transactions.CountAsync();
        Assert.Equal(4,count);
    }

    [Fact]

    public async Task UpdatedField_IsDetectedAndRecorded()
    {
        //arrange
        var db = CreateInMemoryDb();
        var mockService = new MockTransactionService();
        var ingestionService = new IngestionServiceTests(db, mockService);

        // first run - insert original data
        await ingestionService.RunAsync();

        //change the amount on transaction 1001

        var updatedMock = new ManualTransactionService(new List<TransactionDto>
        {
            new(1001, "4111111111111111", "STO-01", "Wireless Mouse", 99.99m, DateTime.UtcNow.AddHours(-2)),
            new(1002, "4000000000000002", "STO-02", "USB-C Cable", 25.00m, DateTime.UtcNow.AddHours(-5)),
            new(1003, "5500000000000004", "STO-01", "HDMI Cable", 15.49m, DateTime.UtcNow.AddHours(-1)),
            new(1004, "4111111111111111", "STO-03", "Keyboard", 45.00m, DateTime.UtcNow.AddHours(-10))
        });

        // act
        var ingestionService2 = new IngestionServiceTests(db, updatedMock);
        await ingestionService2.RunAsynbc();

        //assert

        var audit = await db.TransactionAudits.FirstOrDefaultAsync(a => a.TransactionId == 1001 && a.FieldName == "Amount");

        Assert.NotNull(audit);
        Assert.Equal("19.99", audit.OldValue);
        Assert.Equal("99.99", audit.NewValue);

    }

    [Fact]
    public async Task MissingTransaction_IsRevoked()
    {
        // arrange
        var db = CreateInMemoryDb();
        var mockService = new MockTransactionService();
        var ingestionService = new IngestionServiceTests(db, mockService);

        // first run - insert all transactions
        await ingestionService.RunAsync();
        // second run - transaction 1002 is missing
        var updatedMock = new ManualTransactionService(new List<TransactionDto>
        {
            new(1001, "4111111111111111", "STO-01", "Wireless Mouse", 19.99m, DateTime.UtcNow.AddHours(-2)),
            new(1003, "5500000000000004", "STO-01", "HDMI Cable", 15.49m, DateTime.UtcNow.AddHours(-1)),
            new(1004, "4111111111111111", "STO-03", "Keyboard", 45.00m, DateTime.UtcNow.AddHours(-10))
        });
        // act
        var ingestionService2 = new IngestionServiceTests(db, updatedMock);
        await ingestionService2.RunAsync();

        //assert
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == 1002);

        Assert.NotNull(transaction);
        Assert.Equal(TransactionStatus.Revoked, transaction.Status);
    
    }

    [Fact]
    public async Task RepeatedRun_DoesNotCreateDuplicates()
    {
        // arrange
        var db = CreateInMemoryDb();
        var mockService = new MockTransactionService();
        var ingestionService = new IngestionService(db, mockService);

        // act run twice with same data
        await ingestionService.RunAsync();
        await ingestionService.RunAsync();

        //assert
        var transactionCount = await db.Transactions.CountAsync();
        var auditCount = await db.TransactionAudits.CountAsync();

        Assert.Equal(4, transactionCount);
        Assert.Equal(4, auditCount); // only 4 transacs no duplicates
    }
}