using System.Reflection.Metadata;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TransactionsIngest.data;
using TransactionsIngest.Models;

namespace TransactionsIngest.data;

public class AppDbContext : AppDbContext
{
    public DbSet<Transaction> Transactions {get; set;}
    public DbSet<TransactionAudit> TransactionAudits {get; set;}

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

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

        modelBuilder.Entity<TransactionAudit>(EntityHandle =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}