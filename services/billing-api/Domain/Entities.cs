namespace Korp.Billing.Api.Domain;

public enum InvoiceStatus
{
    Open = 0,
    Closed = 1
}

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Number { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public string? LastIdempotencyKey { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

public class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class InvoiceSequence
{
    public int Id { get; set; } = 1;
    public int LastNumber { get; set; }
}
