using Korp.Billing.Api.Domain;
using Korp.Billing.Api.Errors;
using Korp.Billing.Api.Integrations;
using Korp.Billing.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Korp.Billing.Api.Tests;

[Collection(nameof(BillingPostgresCollection))]
public class AssistantServiceTests(PostgresFixture postgres)
{
    [SkippableFact]
    public async Task Insights_alerta_quando_nota_aberta_pede_mais_que_o_saldo()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var productId = Guid.NewGuid();
        db.Invoices.Add(new Invoice
        {
            Number = 1,
            Status = InvoiceStatus.Open,
            Items =
            [
                new InvoiceItem
                {
                    ProductId = productId,
                    ProductCode = "PAR-M8",
                    ProductDescription = "Parafuso",
                    Quantity = 2
                }
            ]
        });
        await db.SaveChangesAsync();

        var stock = new Mock<IStockGateway>();
        stock.Setup(s => s.ListProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new StockProductDto(productId, "PAR-M8", "Parafuso", 1)]);

        var sut = new AssistantService(db, stock.Object, NullLogger<AssistantService>.Instance);
        var insights = await sut.InsightsAsync(default);

        Assert.Contains(insights, i => i.Title == "Risco de impressão" && i.Severity == "danger");
        Assert.Contains(insights, i => i.Title == "Saldo crítico");
    }

    [SkippableFact]
    public async Task Insights_marca_estoque_indisponivel_quando_gateway_falha()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var stock = new Mock<IStockGateway>();
        stock.Setup(s => s.ListProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(BillingErrors.StockUnavailable("503"));

        var sut = new AssistantService(db, stock.Object, NullLogger<AssistantService>.Instance);
        var insights = await sut.InsightsAsync(default);

        Assert.Contains(insights, i => i.Title == "Estoque indisponível" && i.Severity == "danger");
    }

    [SkippableFact]
    public async Task Ask_responde_roteiro_de_concorrencia()
    {
        Skip.IfNot(postgres.Available, PostgresRequired);
        await using var db = await postgres.OpenCleanDbAsync();
        var sut = new AssistantService(db, new Mock<IStockGateway>().Object, NullLogger<AssistantService>.Instance);

        var reply = await sut.AskAsync("como testar concorrência com o parafuso?", default);

        Assert.Contains("PAR-M8", reply.Answer);
        Assert.Contains(reply.Highlights, h => h.Contains("pg_advisory_xact_lock"));
    }

    private const string PostgresRequired = "PostgreSQL (Docker/Colima ou compose na porta 5433) é necessário.";
}
