using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Domain;
using Korp.Billing.Api.Errors;
using Korp.Billing.Api.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Services;

public class InvoiceService(BillingDbContext db, IStockGateway stock, ILogger<InvoiceService> logger)
{
    public async Task<IReadOnlyList<InvoiceResponse>> ListAsync(CancellationToken ct)
    {
        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .OrderByDescending(i => i.Number)
            .ToListAsync(ct);

        return invoices.Select(ToResponse).ToList();
    }

    public async Task<InvoiceResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking().Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id, ct)
                      ?? throw BillingErrors.NotFound($"Nota fiscal {id} não encontrada.");
        return ToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken ct)
    {
        var items = await BuildItemsAsync(request.Items, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var sequence = await db.Sequences.FirstAsync(ct);
        sequence.LastNumber += 1;

        var invoice = new Invoice
        {
            Number = sequence.LastNumber,
            Status = InvoiceStatus.Open,
            Items = items
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToResponse(invoice);
    }

    public async Task<InvoiceResponse> UpdateAsync(Guid id, UpdateInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id, ct)
                      ?? throw BillingErrors.NotFound($"Nota fiscal {id} não encontrada.");

        if (invoice.Status != InvoiceStatus.Open)
        {
            throw BillingErrors.Conflict("Somente notas com status Aberta podem ser editadas.");
        }

        var items = await BuildItemsAsync(request.Items, ct);
        db.Items.RemoveRange(invoice.Items);
        invoice.Items = items;
        await db.SaveChangesAsync(ct);
        return ToResponse(invoice);
    }

    public async Task<PrintInvoiceResponse> PrintAsync(Guid id, string? idempotencyKey, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id, ct)
                      ?? throw BillingErrors.NotFound($"Nota fiscal {id} não encontrada.");

        if (invoice.Status == InvoiceStatus.Closed)
        {
            return new PrintInvoiceResponse(ToResponse(invoice), true,
                "Nota já estava Fechada. Impressão repetida ignorada (idempotência).");
        }

        if (invoice.Status != InvoiceStatus.Open)
        {
            throw BillingErrors.Conflict("Não é permitido imprimir notas com status diferente de Aberta.");
        }

        if (!invoice.Items.Any())
        {
            throw BillingErrors.Validation("A nota precisa ter ao menos um item para impressão.");
        }

        var applied = new List<(Guid ProductId, decimal Quantity, string OperationId)>();

        try
        {
            foreach (var item in invoice.Items)
            {
                var operationId = $"invoice:{invoice.Id}:product:{item.ProductId}";
                await stock.DebitAsync(item.ProductId, item.Quantity, operationId, ct);
                applied.Add((item.ProductId, item.Quantity, operationId));
            }

            invoice.Status = InvoiceStatus.Closed;
            invoice.ClosedAt = DateTimeOffset.UtcNow;
            invoice.LastIdempotencyKey = idempotencyKey;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Nota {Number} impressa e fechada", invoice.Number);
            return new PrintInvoiceResponse(ToResponse(invoice), false,
                "Impressão concluída. Status atualizado para Fechada e saldos baixados no estoque.");
        }
        catch (Exception)
        {
            foreach (var op in Enumerable.Reverse(applied))
            {
                try
                {
                    await stock.CreditAsync(op.ProductId, op.Quantity, op.OperationId, ct);
                }
                catch (Exception compensateEx)
                {
                    logger.LogError(compensateEx, "Falha ao compensar baixa {OperationId}", op.OperationId);
                }
            }

            throw;
        }
    }

    private async Task<List<InvoiceItem>> BuildItemsAsync(IReadOnlyList<InvoiceItemRequest>? items, CancellationToken ct)
    {
        if (items is null || items.Count == 0)
        {
            throw BillingErrors.Validation("Informe ao menos um produto na nota fiscal.");
        }

        var grouped = items
            .GroupBy(i => i.ProductId)
            .Select(g => new InvoiceItemRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var result = new List<InvoiceItem>();
        foreach (var item in grouped)
        {
            if (item.Quantity <= 0)
            {
                throw BillingErrors.Validation("Quantidade do item deve ser maior que zero.");
            }

            var product = await stock.GetProductAsync(item.ProductId, ct);
            result.Add(new InvoiceItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductDescription = product.Description,
                Quantity = item.Quantity
            });
        }

        return result;
    }

    private static InvoiceResponse ToResponse(Invoice invoice) => new(
        invoice.Id,
        invoice.Number,
        invoice.Status == InvoiceStatus.Open ? "Aberta" : "Fechada",
        invoice.CreatedAt,
        invoice.ClosedAt,
        invoice.Items
            .OrderBy(i => i.ProductCode)
            .Select(i => new InvoiceItemResponse(i.Id, i.ProductId, i.ProductCode, i.ProductDescription, i.Quantity))
            .ToList());
}
