# Transactions Ingest

## Overview
An hourly ingestion job that captures the last 24 hours, reconciles late arrivals, and preserves an audit trail of changes.
The app fetches the last‑24‑hour snapshot, upserts records by transaction ID, detects and records updates, marks revoked transactions (those missing from the current snapshot while still within 24 hours), and may finalize records older than 24 hours.
The Console app will run once per hour by an external scheduler. 

## Building and Running Steps/Test
dotnet restore
dotnet build
dotnet run --project TransactionsIngest

dotnet test

## Assumptions
## Time Tracking
Estimated: 12 hours
Actual: 