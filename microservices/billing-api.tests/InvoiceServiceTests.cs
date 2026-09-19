using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Domain;
using Korp.Billing.Api.Errors;
using Korp.Billing.Api.Integrations;
using Korp.Billing.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Korp.Billing.Api.Tests;

[Collection(nameof(BillingPostgresCollection))]
public class InvoiceServiceTests(PostgresFixture postgres)
{
    [SkippableFact]
    public async Task Create_gera_numeros_sequenciais()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var product = Product("ACO-1020");
        var stock = StockMock(product);
        var sut = new InvoiceService(db, stock.Object, NullLogger<InvoiceService>.Instance);

        var first = await sut.CreateAsync(new CreateInvoiceRequest([new InvoiceItemRequest(product.Id, 2)]), default);
        var second = await sut.CreateAsync(new CreateInvoiceRequest([new InvoiceItemRequest(product.Id, 1)]), default);

        Assert.Equal(1, first.Number);
        Assert.Equal(2, second.Number);
        Assert.Equal("Aberta", first.Status);
        Assert.Equal("Aberta", second.Status);
        stock.Verify(s => s.DebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [SkippableFact]
    public async Task Create_agrupa_itens_do_mesmo_produto()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var product = Product("PAR-M8");
        var sut = new InvoiceService(db, StockMock(product).Object, NullLogger<InvoiceService>.Instance);

        var invoice = await sut.CreateAsync(new CreateInvoiceRequest(
        [
            new InvoiceItemRequest(product.Id, 1),
            new InvoiceItemRequest(product.Id, 2)
        ]), default);

        Assert.Single(invoice.Items);
        Assert.Equal(3, invoice.Items[0].Quantity);
    }

    [SkippableFact]
    public async Task Print_fecha_nota_e_baixa_estoque()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var product = Product("ACO-1020");
        var stock = StockMock(product);
        var sut = new InvoiceService(db, stock.Object, NullLogger<InvoiceService>.Instance);
        var created = await sut.CreateAsync(new CreateInvoiceRequest([new InvoiceItemRequest(product.Id, 2)]), default);

        var printed = await sut.PrintAsync(created.Id, "print-1", default);

        Assert.False(printed.IdempotentReplay);
        Assert.Equal("Fechada", printed.Invoice.Status);
        Assert.NotNull(printed.Invoice.ClosedAt);
        stock.Verify(s => s.DebitAsync(product.Id, 2, $"invoice:{created.Id}:product:{product.Id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [SkippableFact]
    public async Task Print_nota_fechada_e_idempotente_e_nao_baixa_de_novo()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var product = Product("ACO-1020");
        var stock = StockMock(product);
        var sut = new InvoiceService(db, stock.Object, NullLogger<InvoiceService>.Instance);
        var created = await sut.CreateAsync(new CreateInvoiceRequest([new InvoiceItemRequest(product.Id, 2)]), default);
        await sut.PrintAsync(created.Id, "print-1", default);

        var reprint = await sut.PrintAsync(created.Id, "print-1", default);

        Assert.True(reprint.IdempotentReplay);
        Assert.Equal("Fechada", reprint.Invoice.Status);
        stock.Verify(s => s.DebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [SkippableFact]
    public async Task Print_compensa_baixas_quando_estoque_falha_no_segundo_item()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var first = Product("ACO-1020");
        var second = Product("PAR-M8");
        var stock = new Mock<IStockGateway>();
        stock.Setup(s => s.GetProductAsync(first.Id, It.IsAny<CancellationToken>())).ReturnsAsync(first);
        stock.Setup(s => s.GetProductAsync(second.Id, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        var debitCalls = 0;
        stock.Setup(s => s.DebitAsync(It.IsAny<Guid>(), 1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                debitCalls++;
                if (debitCalls > 1)
                {
                    throw BillingErrors.InsufficientStock("saldo 0");
                }
            });
        stock.Setup(s => s.CreditAsync(It.IsAny<Guid>(), 1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new InvoiceService(db, stock.Object, NullLogger<InvoiceService>.Instance);
        var created = await sut.CreateAsync(new CreateInvoiceRequest(
        [
            new InvoiceItemRequest(first.Id, 1),
            new InvoiceItemRequest(second.Id, 1)
        ]), default);

        var ex = await Assert.ThrowsAsync<AppException>(() => sut.PrintAsync(created.Id, "print-1", default));

        Assert.Equal("INSUFFICIENT_STOCK", ex.Code);
        stock.Verify(s => s.CreditAsync(It.IsAny<Guid>(), 1, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        var stored = await db.Invoices.AsNoTracking().SingleAsync(i => i.Id == created.Id);
        Assert.Equal(InvoiceStatus.Open, stored.Status);
        Assert.Null(stored.ClosedAt);
    }

    [SkippableFact]
    public async Task Update_rejeita_nota_fechada()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var product = Product("ACO-1020");
        var sut = new InvoiceService(db, StockMock(product).Object, NullLogger<InvoiceService>.Instance);
        var created = await sut.CreateAsync(new CreateInvoiceRequest([new InvoiceItemRequest(product.Id, 1)]), default);
        await sut.PrintAsync(created.Id, "print-1", default);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            sut.UpdateAsync(created.Id, new UpdateInvoiceRequest([new InvoiceItemRequest(product.Id, 2)]), default));

        Assert.Equal("CONFLICT", ex.Code);
    }

    private const string PostgresRequired = "PostgreSQL (Docker/Colima ou compose na porta 5433) é necessário.";

    private static StockProductDto Product(string code) =>
        new(Guid.NewGuid(), code, code, 10);

    private static Mock<IStockGateway> StockMock(StockProductDto product)
    {
        var stock = new Mock<IStockGateway>();
        stock.Setup(s => s.GetProductAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        stock.Setup(s => s.DebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        stock.Setup(s => s.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return stock;
    }
}
