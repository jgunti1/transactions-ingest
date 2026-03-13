namespace TransactionsIngest.Services;

public class ManualTransactionService : ITransactionService
{
    private readonly List<TransactionDto> _transactions;
    public ManualTransactionService(List<TransactionDto> transactions)
    {
        _transactions = transactions;

    }

    public Task<IEnumerable<TransactionDto>> FetchTransactionsAsync()
    {
        return Task.FromResult<IEnumerable<TransactionDto>>(_transactions);
    }
}