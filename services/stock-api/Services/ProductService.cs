using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Data;
using Korp.Stock.Api.Domain;
using Korp.Stock.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace Korp.Stock.Api.Services;

public class ProductService(StockDbContext db, ILogger<ProductService> logger)
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(string? search, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Code.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        return await query
            .OrderBy(p => p.Code)
            .Select(p => new ProductResponse(p.Id, p.Code, p.Description, p.Balance, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<ProductResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw StockErrors.NotFound($"Produto {id} não encontrado.");

        return ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        Validate(request.Code, request.Description, request.Balance);

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await db.Products.AnyAsync(p => p.Code == code, ct);
        if (exists)
        {
            throw StockErrors.Conflict($"Já existe um produto com o código {code}.");
        }

        var product = new Product
        {
            Code = code,
            Description = request.Description.Trim(),
            Balance = request.Balance
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        Validate("X", request.Description, request.Balance);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw StockErrors.NotFound($"Produto {id} não encontrado.");

        product.Description = request.Description.Trim();
        product.Balance = request.Balance;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToResponse(product);
    }

    public async Task<StockOperationResponse> DebitAsync(Guid productId, StockOperationRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
        {
            throw StockErrors.Validation("A quantidade a baixar deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            throw StockErrors.Validation("operationId é obrigatório para garantir idempotência.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var lockKey = BitConverter.ToInt64(productId.ToByteArray(), 0);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", ct);

        var existing = await db.Movements.FirstOrDefaultAsync(m => m.OperationId == request.OperationId, ct);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw StockErrors.NotFound($"Produto {productId} não encontrado.");

        if (existing is not null)
        {
            logger.LogInformation("Replay idempotente da baixa {OperationId} no produto {Code}", request.OperationId, product.Code);
            await tx.CommitAsync(ct);
            return new StockOperationResponse(product.Id, product.Code, product.Balance, request.OperationId, true);
        }

        if (product.Balance < request.Quantity)
        {
            throw StockErrors.InsufficientStock(
                $"Produto {product.Code} possui saldo {product.Balance}; solicitado {request.Quantity}.");
        }

        product.Balance -= request.Quantity;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        db.Movements.Add(new StockMovement
        {
            ProductId = product.Id,
            OperationId = request.OperationId,
            Type = "Debit",
            Quantity = request.Quantity
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Baixa de {Qty} em {Code}. Novo saldo {Balance}", request.Quantity, product.Code, product.Balance);
        return new StockOperationResponse(product.Id, product.Code, product.Balance, request.OperationId, false);
    }

    public async Task<StockOperationResponse> CreditAsync(Guid productId, StockOperationRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
        {
            throw StockErrors.Validation("A quantidade a creditar deve ser maior que zero.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var lockKey = BitConverter.ToInt64(productId.ToByteArray(), 0);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", ct);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw StockErrors.NotFound($"Produto {productId} não encontrado.");

        var original = await db.Movements.FirstOrDefaultAsync(m => m.OperationId == request.OperationId, ct);
        if (original is null)
        {
            await tx.CommitAsync(ct);
            return new StockOperationResponse(product.Id, product.Code, product.Balance, request.OperationId, true);
        }

        product.Balance += original.Quantity;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        db.Movements.Remove(original);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new StockOperationResponse(product.Id, product.Code, product.Balance, request.OperationId, false);
    }

    private static void Validate(string code, string description, decimal balance)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw StockErrors.Validation("Código do produto é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw StockErrors.Validation("Descrição do produto é obrigatória.");
        }

        if (balance < 0)
        {
            throw StockErrors.Validation("Saldo não pode ser negativo.");
        }
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Code, product.Description, product.Balance, product.CreatedAt, product.UpdatedAt);
}
