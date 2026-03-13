# Transactions Ingest

## Overview
An hourly ingestion job that captures the last 24 hours, reconciles late arrivals, and preserves an audit trail of changes.
The app fetches the last‑24‑hour snapshot, upserts records by transaction ID, detects and records updates, marks revoked transactions (those missing from the current snapshot while still within 24 hours), and may finalize records older than 24 hours.
The Console app will run once per hour by an external scheduler. 

## Building and Running Steps/Test
cd TransactionsIngest/TransactionsIngest

dotnet restore
dotnet build
dotnet run 

dotnet test
## Approach & Design Decisions

**Data Model**
The `Transactions` table holds the current state of each 
transaction. The `TransactionAudits` table records every change with old and 
new values. This is so we have a full history of what happened.

**Privacy**
Only the last 4 digits of card numbers are stored.

**Upsert Logic**
Each incoming transaction is looked up by TransactionId. If it does not exist 
it is inserted. If it exists, each field is compared and any changes 
are recorded in the audit table.

**Revocation**
After processing all incoming transactions, any record within the 24 hour 
window that is absent from the current pull is Revoked.

**Idempotency**
Each run is wrapped in a single database transaction. If anything fails, 
the entire run rolls back. Repeated runs with unchanged data produce no 
duplicates or wrong audit entries.

**Dependency Injection**
Services uses Microsoft.Extensions.DependencyInjection. 
The ingestion service depends on ITransactionService rather than the 
 hardcoded values in the mock file, making it easier to put in a real API.

## Assumptions

- A revoked transaction that reappears in a later snapshot is re-activated
- Only storing last 4 of card numbers
- No scheduler, assuming that there is an outside one to call mine every hour
- Mark records older than 24 hours as finalized to stop changing records

## Time Tracking

- Estimated: 12 hours
- Actual: 9 hours


## Testing the Key Scenarios

### Run the app

cd TransactionsIngest
dotnet run

Check `transactions.db`  to see tables.

### Test update detection
In `TransactionsIngest/Services/MockTransactionService.cs`, change the 
amount on transaction 1001 from `19.99m` to a different value. Run 
`dotnet run` again and check the `TransactionAudits` table for an 
"Updated" entry showing the old and new values.

### Test revocation
In `MockTransactionService.cs`, remove one transaction from the list. 
Run `dotnet run` and check the `Transactions` table — the removed 
transaction should show `Status = 1` (Revoked).

### Test idempotency
Run `dotnet run` twice without changing anything. The number of records 
in both tables should remain the same after the second run.

### Run automated tests

cd ..
dotnet test