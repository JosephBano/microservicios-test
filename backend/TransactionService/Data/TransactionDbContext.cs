using Microsoft.EntityFrameworkCore;
using TransactionService.Models;

namespace TransactionService.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("transaction_schema");

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.UnitPrice).HasColumnType("numeric(12,2)");
            entity.Property(t => t.TotalPrice).HasColumnType("numeric(12,2)");
            entity.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(t => t.Date).HasDefaultValueSql("NOW()");
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(t => t.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(t => t.Type)
                  .HasConversion<string>();
        });
    }
}
