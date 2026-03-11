namespace TransactionsIngest.Models;

public class TransactionAudit
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // "Created", "Updated", "Revoked"
    public DateTime ChangedAt { get; set; }
}
