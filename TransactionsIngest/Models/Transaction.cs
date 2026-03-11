namespace TransactionsIngest.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    public string CardLast4 { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public DateTime LastUpdated { get; set; }
}

public enum TransactionStatus
{
    Active,
    Revoked,
    Finalized
}