using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Data;
using Korp.Stock.Api.Domain;
using Korp.Stock.Api.Errors;
using Korp.Stock.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Korp.Stock.Api.Tests;

[Collection(nameof(PostgresCollection))]
public class ProductServiceTests(PostgresFixture postgres)
{
    [SkippableFact]
    public async Task Create_normaliza_codigo_e_lista_por_busca()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);

        var created = await sut.CreateAsync(new CreateProductRequest(" aco-1020 ", "Barra de aço", 10), default);

        Assert.Equal("ACO-1020", created.Code);
        Assert.Equal(10, created.Balance);

        var found = await sut.ListAsync("aço", default);
        Assert.Single(found);
        Assert.Equal(created.Id, found[0].Id);
    }

    [SkippableFact]
    public async Task Create_rejeita_codigo_duplicado()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);

        await sut.CreateAsync(new CreateProductRequest("PAR-M8", "Parafuso", 1), default);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.CreateAsync(new CreateProductRequest("par-m8", "Outro", 2), default));

        Assert.Equal("CONFLICT", ex.Code);
    }

    [SkippableTheory]
    [InlineData("", "desc", 1)]
    [InlineData("X", "", 1)]
    [InlineData("X", "desc", -1)]
    public async Task Create_valida_campos(string code, string description, decimal balance)
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.CreateAsync(new CreateProductRequest(code, description, balance), default));

        Assert.Equal("VALIDATION", ex.Code);
    }

    [SkippableFact]
    public async Task Debit_baixa_saldo_e_replay_nao_baixa_de_novo()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);
        var product = await SeedAsync(db, "ACO-1020", 10);

        var first = await sut.DebitAsync(product.Id, new StockOperationRequest(2, "invoice:1:product:a"), default);
        var replay = await sut.DebitAsync(product.Id, new StockOperationRequest(2, "invoice:1:product:a"), default);

        Assert.False(first.IdempotentReplay);
        Assert.Equal(8, first.Balance);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(8, replay.Balance);
        Assert.Equal(8, (await sut.GetAsync(product.Id, default)).Balance);
    }

    [SkippableFact]
    public async Task Debit_saldo_insuficiente()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);
        var product = await SeedAsync(db, "PAR-M8", 1);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.DebitAsync(product.Id, new StockOperationRequest(2, "op-1"), default));

        Assert.Equal("INSUFFICIENT_STOCK", ex.Code);
        Assert.Equal(1, (await sut.GetAsync(product.Id, default)).Balance);
    }

    [SkippableFact]
    public async Task Credit_restaura_saldo_e_remove_movimento()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new ProductService(db, NullLogger<ProductService>.Instance);
        var product = await SeedAsync(db, "ACO-1020", 10);

        await sut.DebitAsync(product.Id, new StockOperationRequest(2, "op-debit"), default);
        var credit = await sut.CreditAsync(product.Id, new StockOperationRequest(2, "op-debit"), default);
        var replay = await sut.CreditAsync(product.Id, new StockOperationRequest(2, "op-debit"), default);

        Assert.False(credit.IdempotentReplay);
        Assert.Equal(10, credit.Balance);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(10, replay.Balance);
    }

    private static async Task<Product> SeedAsync(StockDbContext db, string code, decimal balance)
    {
        var product = new Product { Code = code, Description = code, Balance = balance };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private const string PostgresRequired = "PostgreSQL (Docker/Colima ou compose na porta 5433) é necessário.";
}
