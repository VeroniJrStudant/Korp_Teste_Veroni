using Korp.Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Data;

public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> Items => Set<InvoiceItem>();
    public DbSet<InvoiceSequence> Sequences => Set<InvoiceSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.Number).IsUnique();
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasMany(i => i.Items).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("invoice_items");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductCode).HasMaxLength(40).IsRequired();
            entity.Property(i => i.ProductDescription).HasMaxLength(240).IsRequired();
            entity.Property(i => i.Quantity).HasPrecision(18, 3);
        });

        modelBuilder.Entity<InvoiceSequence>(entity =>
        {
            entity.ToTable("invoice_sequences");
            entity.HasKey(s => s.Id);
        });
    }
}
