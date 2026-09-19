using Korp.Stock.Api.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Stock.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Erro de negócio {Code}", ex.Code);
            await WriteAsync(context, ex.StatusCode, ex.Title, ex.Message, ex.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado no serviço de estoque");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Erro interno",
                "Falha inesperada no serviço de estoque. Tente novamente.", "INTERNAL");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string title, string detail, string code)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://korp.local/errors/{code.ToLowerInvariant()}",
            Extensions = { ["code"] = code }
        });
    }
}
