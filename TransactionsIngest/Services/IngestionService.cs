using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Data;
using TransactionsIngest.Models;

namespace TransactionsIngest.Services;

public class IngestionService
{
    private readonly AppDbContext _db;
    private readonly ITransactionService _transactionService;

    public IngestionService(AppDbContext db, ITransactionService transactionService)
    {
        _db = db;
        _transactionService = transactionService;
    }
    public async Task RunAsync()
    {
        //fetch transactions from the "API" (hardcoded json values)
        var incoming = await _transactionService.FetchTransactionsAsync();
        var incomingList = incoming.ToList();
        var incomingIds = incomingList.Select(t => t.TransactionId).ToHashSet();
        var cutoff = DateTime.UtcNow.AddHours(-24);

        // Wrap everything in a single DB transaction for idempotency
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();

        try
        {
            //Upsert each incoming transaction
            foreach (var dto in incomingList)
            {
                var existing = await _db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == dto.TransactionId);
                // for privacy only get the last 4 of the card
                var cardLast4 = dto.CardNumber.Length  >= 4 
                    ? dto.CardNumber[^4..]
                    : dto.CardNumber;
                if (existing == null)
                {
                    //Insert new transaction
                    var newTransaction = new Transaction
                    {
                        TransactionId = dto.TransactionId,
                        CardLast4 = cardLast4,
                        LocationCode = dto.LocationCode,
                        ProductName = dto.ProductName,
                        Amount = dto.Amount,
                        TransactionTime = dto.Timestamp,
                        Status = TransactionStatus.Active,
                        LastUpdated = DateTime.UtcNow
                    };

                    _db.Transactions.Add(newTransaction);
                    
                    _db.TransactionAudits.Add(new TransactionAudit
                    {
                         TransactionId = dto.TransactionId,
                         FieldName = "All",
                         OldValue = string.Empty,
                         NewValue = $"Created: Amount={dto.Amount}, Product={dto.ProductName}",
                         ChangeType = "Created",
                         ChangedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    //check fo rchanges and record them
                    // transactionid already exists so we just need to keep it for the audit trail
                    DetectAndRecordChanges(existing,dto,cardLast4);

                    existing.Status = TransactionStatus.Active;
                    existing.LastUpdated = DateTime.UtcNow;
                }
            }

            // revocation: mark missing transactions as revoked
            var activeWithinWindow = await _db.Transactions.Where(t => t.TransactionTime >= cutoff
                        && t.Status == TransactionStatus.Active).ToListAsync();
            
            foreach (var transaction in activeWithinWindow)
            {
                if (!incomingIds.Contains(transaction.TransactionId))
                {
                    transaction.Status = TransactionStatus.Revoked;
                    transaction.LastUpdated = DateTime.UtcNow;

                    _db.TransactionAudits.Add(new TransactionAudit
                    {
                        TransactionId = transaction.TransactionId,
                        FieldName = "Status",
                        OldValue = "Active",
                        NewValue = "Revoked",
                        ChangeType = "Revoked",
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
            // finaliztion: lock records older than 24 hours
            var toFinalize = await _db.Transactions.Where(t => t.TransactionTime < cutoff
                            && t.Status == TransactionStatus.Active).ToListAsync();

            foreach (var transaction in toFinalize)
            {
                transaction.Status = TransactionStatus.Finalized;
                transaction.LastUpdated = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            Console.WriteLine($"Ingestion complete. Processed {incomingList.Count} transactions.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            Console.WriteLine($"Ingestion failed: {ex.Message}");
            throw;
        }
    }
    private void DetectAndRecordChanges(Transaction existing, TransactionDto dto, string cardLast4)
    {
        if (existing.Amount != dto.Amount)
            RecordChange(existing.TransactionId,"Amount",existing.Amount.ToString(), dto.Amount.ToString());

        if (existing.ProductName != dto.ProductName)
            RecordChange(existing.TransactionId, "ProductName",existing.ProductName, dto.ProductName);
        
        if (existing.LocationCode != dto.LocationCode)
            RecordChange(existing.TransactionId, "LocationCode",existing.LocationCode, dto.LocationCode);

        if (existing.CardLast4 != cardLast4)
            RecordChange(existing.TransactionId, "CardLast4",existing.CardLast4, cardLast4);

        existing.Amount = dto.Amount;
        existing.ProductName = dto.ProductName;
        existing.LocationCode = dto.LocationCode;
        existing.CardLast4 = cardLast4;

    }
    
    private void RecordChange(int transactionId, string fieldName, string oldValue, string newValue)
    {
        _db.TransactionAudits.Add(new TransactionAudit
        {
            TransactionId = transactionId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangeType = "Updated",
            ChangedAt = DateTime.UtcNow
        });
    }
}


    
