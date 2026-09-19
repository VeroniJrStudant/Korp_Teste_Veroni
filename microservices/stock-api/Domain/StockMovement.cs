namespace Korp.Stock.Api.Domain;

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string OperationId { get; set; } = string.Empty;
    public string Type { get; set; } = "Debit";
    public decimal Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
