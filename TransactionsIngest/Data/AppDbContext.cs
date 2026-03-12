using SystemTransaction = System.Transactions.Transaction;
using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Models;

namespace TransactionsIngest.Data;

public class AppDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionAudit> TransactionAudits { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.CardLast4).HasMaxLength(4);
            entity.Property(e => e.LocationCode).HasMaxLength(20);
            entity.Property(e => e.ProductName).HasMaxLength(20);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<TransactionAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}