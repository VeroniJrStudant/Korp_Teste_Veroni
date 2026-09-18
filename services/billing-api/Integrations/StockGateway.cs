using System.Net.Http.Json;
using System.Net;
using Korp.Billing.Api.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Billing.Api.Integrations;

public record StockProductDto(Guid Id, string Code, string Description, decimal Balance);

public record StockOperationRequest(decimal Quantity, string OperationId);

public interface IStockGateway
{
    Task<StockProductDto> GetProductAsync(Guid productId, CancellationToken ct);
    Task<IReadOnlyList<StockProductDto>> ListProductsAsync(CancellationToken ct);
    Task DebitAsync(Guid productId, decimal quantity, string operationId, CancellationToken ct);
    Task CreditAsync(Guid productId, decimal quantity, string operationId, CancellationToken ct);
}

public class StockGateway(HttpClient http, ILogger<StockGateway> logger) : IStockGateway
{
    public async Task<StockProductDto> GetProductAsync(Guid productId, CancellationToken ct)
    {
        try
        {
            var response = await http.GetAsync($"/api/products/{productId}", ct);
            await EnsureSuccess(response, "consultar produto");
            var product = await response.Content.ReadFromJsonAsync<StockProductDto>(ct);
            return product ?? throw BillingErrors.StockUnavailable("Resposta vazia do serviço de estoque.");
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao consultar produto {ProductId} no estoque", productId);
            throw BillingErrors.StockUnavailable(
                "Não foi possível consultar o estoque. A operação foi interrompida e pode ser tentada novamente.");
        }
    }

    public async Task<IReadOnlyList<StockProductDto>> ListProductsAsync(CancellationToken ct)
    {
        try
        {
            var response = await http.GetAsync("/api/products", ct);
            await EnsureSuccess(response, "listar produtos");
            var products = await response.Content.ReadFromJsonAsync<List<StockProductDto>>(ct);
            return products ?? [];
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao listar produtos no estoque");
            throw BillingErrors.StockUnavailable(
                "Não foi possível consultar o estoque. A operação foi interrompida e pode ser tentada novamente.");
        }
    }

    public Task DebitAsync(Guid productId, decimal quantity, string operationId, CancellationToken ct) =>
        PostOperation(productId, quantity, operationId, "debit", ct);

    public Task CreditAsync(Guid productId, decimal quantity, string operationId, CancellationToken ct) =>
        PostOperation(productId, quantity, operationId, "credit", ct);

    private async Task PostOperation(Guid productId, decimal quantity, string operationId, string action, CancellationToken ct)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                $"/api/products/{productId}/{action}",
                new StockOperationRequest(quantity, operationId),
                ct);
            await EnsureSuccess(response, action == "debit" ? "baixar estoque" : "estornar estoque");
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao {Action} produto {ProductId}", action, productId);
            throw BillingErrors.StockUnavailable(
                "O microsserviço de estoque falhou ou está indisponível. A nota permanece Aberta.");
        }
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problem = await TryReadProblem(response);
        var detail = problem?.Detail ?? $"Falha ao {operation} (HTTP {(int)response.StatusCode}).";

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw BillingErrors.InsufficientStock(detail);
        }

        if (response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.RequestTimeout)
        {
            throw BillingErrors.StockUnavailable(detail);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw BillingErrors.NotFound(detail);
        }

        throw BillingErrors.StockUnavailable(detail);
    }

    private static async Task<ProblemDetails?> TryReadProblem(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch
        {
            return null;
        }
    }
}
