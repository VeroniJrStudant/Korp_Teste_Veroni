using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Domain;
using Korp.Stock.Api.Errors;
using Korp.Stock.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Korp.Stock.Api.Tests;

[Collection(nameof(PostgresCollection))]
[Trait("Category", "Integration")]
public class ProductServiceIntegrationTests(PostgresFixture postgres)
{
    [SkippableFact]
    public async Task Debit_idempotente_nao_altera_saldo_no_postgres()
    {
        Skip.IfNot(postgres.Available, "PostgreSQL (Docker/Colima ou compose na porta 5433) é necessário.");
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);
        var product = await SeedAsync(db, "ACO-1020", 10);

        await sut.DebitAsync(product.Id, new StockOperationRequest(2, "invoice:nf:product:aco"), default);
        var replay = await sut.DebitAsync(product.Id, new StockOperationRequest(2, "invoice:nf:product:aco"), default);

        Assert.True(replay.IdempotentReplay);
        Assert.Equal(8, replay.Balance);
        Assert.Equal(1, await db.Movements.CountAsync());
    }

    [SkippableFact]
    public async Task Concorrencia_so_uma_baixa_vence_quando_saldo_e_1()
    {
        Skip.IfNot(postgres.Available, "PostgreSQL (Docker/Colima ou compose na porta 5433) é necessário.");
        await using var setup = await postgres.OpenCleanDbAsync();
        var product = await SeedAsync(setup, "PAR-M8", 1);
        var productId = product.Id;

        var tasks = Enumerable.Range(1, 2).Select(async i =>
        {
            await using var db = postgres.CreateDb();
            var sut = new ProductService(db, NullLogger<ProductService>.Instance);
            try
            {
                var result = await sut.DebitAsync(productId, new StockOperationRequest(1, $"print-{i}"), default);
                return (Ok: true, result.Balance, Error: (string?)null);
            }
            catch (AppException ex)
            {
                return (Ok: false, Balance: 0m, Error: ex.Code);
            }
        });

        var outcomes = await Task.WhenAll(tasks);

        Assert.Single(outcomes, o => o.Ok);
        Assert.Single(outcomes, o => !o.Ok && o.Error == "INSUFFICIENT_STOCK");

        await using var verify = postgres.CreateDb();
        var stored = await verify.Products.SingleAsync(p => p.Id == productId);
        Assert.Equal(0, stored.Balance);
        Assert.Equal(1, await verify.Movements.CountAsync());
    }

    private static async Task<Product> SeedAsync(Korp.Stock.Api.Data.StockDbContext db, string code, decimal balance)
    {
        var product = new Product { Code = code, Description = code, Balance = balance };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
