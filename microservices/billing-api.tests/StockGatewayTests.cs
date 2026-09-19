using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Korp.Billing.Api.Errors;
using Korp.Billing.Api.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Korp.Billing.Api.Tests;

public class StockGatewayTests
{
    [Fact]
    public async Task GetProduct_mapeia_409_para_saldo_insuficiente()
    {
        var gateway = CreateGateway(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(new ProblemDetails
            {
                Detail = "Produto PAR-M8 possui saldo 0; solicitado 1."
            })
        });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            gateway.DebitAsync(Guid.NewGuid(), 1, "op-1", default));

        Assert.Equal("INSUFFICIENT_STOCK", ex.Code);
        Assert.Contains("PAR-M8", ex.Message);
    }

    [Fact]
    public async Task GetProduct_mapeia_503_para_estoque_indisponivel()
    {
        var gateway = CreateGateway(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            gateway.GetProductAsync(Guid.NewGuid(), default));

        Assert.Equal("STOCK_UNAVAILABLE", ex.Code);
        Assert.Equal(503, ex.StatusCode);
    }

    [Fact]
    public async Task GetProduct_deserializa_produto()
    {
        var id = Guid.NewGuid();
        var gateway = CreateGateway(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new StockProductDto(id, "ACO-1020", "Barra", 10)),
                Encoding.UTF8,
                "application/json")
        });

        var product = await gateway.GetProductAsync(id, default);

        Assert.Equal("ACO-1020", product.Code);
        Assert.Equal(10, product.Balance);
    }

    private static StockGateway CreateGateway(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var http = new HttpClient(new StubHandler(responder))
        {
            BaseAddress = new Uri("http://stock.test")
        };
        return new StockGateway(http, NullLogger<StockGateway>.Instance);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
