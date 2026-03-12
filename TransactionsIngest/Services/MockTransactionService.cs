using TransactionsIngest.Services;

namespace TransactionsIngest.Services;

public record TransactionDto(
    int TransactionId,
    string CardNumber,
    string LocationCode,
    string ProductName,
    decimal Amount,
    DateTime Timestamp
);
public class MockTransactionService : ITransactionService
{
    public Task<IEnumerable<TransactionDto>> FetchTransactionsAsync()
    {
        var transactions = new List<TransactionDto>
        // hardcoded values
        {
             new(1001, "4111111111111111", "STO-01", "Wireless Mouse", 24.99m, DateTime.UtcNow.AddHours(-2)),
            //new(1002, "4000000000000002", "STO-02", "USB-C Cable", 25.00m, DateTime.UtcNow.AddHours(-5)),
            new(1003, "5500000000000004", "STO-01", "HDMI Cable", 15.49m, DateTime.UtcNow.AddHours(-1)),
            new(1004, "4111111111111111", "STO-03", "Keyboard", 45.00m, DateTime.UtcNow.AddHours(-10))

        };

        return Task.FromResult<IEnumerable<TransactionDto>>(transactions);
    }
}