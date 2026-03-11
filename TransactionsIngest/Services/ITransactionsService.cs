namespace TransactionsIngest.Services;

public interface ITransactionService
{
    Task<IEnumerable<TransactionDto>> FetchTransactionsAsync();
    
}