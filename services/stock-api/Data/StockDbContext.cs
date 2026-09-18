using Korp.Stock.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Korp.Stock.Api.Data;

public class StockDbContext(DbContextOptions<StockDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> Movements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Code).HasMaxLength(40).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(240).IsRequired();
            entity.Property(p => p.Balance).HasPrecision(18, 3);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.OperationId).IsUnique();
            entity.Property(m => m.OperationId).HasMaxLength(120).IsRequired();
            entity.Property(m => m.Type).HasMaxLength(20).IsRequired();
            entity.Property(m => m.Quantity).HasPrecision(18, 3);
            entity.HasOne(m => m.Product)
                .WithMany(p => p.Movements)
                .HasForeignKey(m => m.ProductId);
        });
    }
}
