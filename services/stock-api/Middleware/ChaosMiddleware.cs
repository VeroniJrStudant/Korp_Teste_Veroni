using Korp.Stock.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Korp.Stock.Api.Middleware;

public class ChaosMiddleware(RequestDelegate next, ChaosState chaos)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isBypass = path.Contains("/health", StringComparison.OrdinalIgnoreCase)
                       || path.Contains("/chaos", StringComparison.OrdinalIgnoreCase)
                       || path.Contains("/swagger", StringComparison.OrdinalIgnoreCase);

        if (chaos.Enabled && !isBypass)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 503,
                Title = "Serviço de estoque indisponível",
                Detail = "Falha simulada no microsserviço de estoque. O faturamento deve se recuperar e a nota permanecer Aberta.",
                Type = "https://korp.local/errors/chaos",
                Extensions = { ["code"] = "STOCK_UNAVAILABLE" }
            });
            return;
        }

        await next(context);
    }
}
