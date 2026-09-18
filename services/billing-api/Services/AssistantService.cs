using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Domain;
using Korp.Billing.Api.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Services;

public class AssistantService(BillingDbContext db, IStockGateway stock, ILogger<AssistantService> logger)
{
    public async Task<IReadOnlyList<InsightResponse>> InsightsAsync(CancellationToken ct)
    {
        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Items).ToListAsync(ct);
        var insights = new List<InsightResponse>();

        var open = invoices.Where(i => i.Status == InvoiceStatus.Open).ToList();
        var closed = invoices.Where(i => i.Status == InvoiceStatus.Closed).ToList();

        insights.Add(new InsightResponse(
            "Fila de faturamento",
            open.Count == 0
                ? "Não há notas abertas no momento."
                : $"{open.Count} nota(s) Aberta(s) aguardando impressão. A baixa de estoque só ocorre na impressão.",
            open.Count > 0 ? "warning" : "ok"));

        insights.Add(new InsightResponse(
            "Notas fechadas",
            $"{closed.Count} nota(s) já impressas e fechadas. Reimpressão é idempotente e não baixa o estoque de novo.",
            "info"));

        try
        {
            var products = await stock.ListProductsAsync(ct);
            var critical = products.Where(p => p.Balance <= 1).ToList();
            insights.Add(new InsightResponse(
                "Saldo crítico",
                critical.Count == 0
                    ? "Nenhum produto com saldo crítico."
                    : string.Join(" · ", critical.Select(p => $"{p.Code} ({p.Balance})")),
                critical.Count == 0 ? "ok" : "danger"));

            var demanded = invoices
                .Where(i => i.Status == InvoiceStatus.Open)
                .SelectMany(i => i.Items)
                .GroupBy(i => i.ProductCode)
                .Select(g => new { Code = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToList();

            var conflicts = demanded
                .Join(products, d => d.Code, p => p.Code, (d, p) => new { d.Code, d.Qty, p.Balance })
                .Where(x => x.Qty > x.Balance)
                .ToList();

            if (conflicts.Count > 0)
            {
                insights.Add(new InsightResponse(
                    "Risco de impressão",
                    "Notas abertas exigem mais saldo do que o estoque atual: " +
                    string.Join(" · ", conflicts.Select(c => $"{c.Code} pede {c.Qty}, saldo {c.Balance}")),
                    "danger"));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Estoque indisponível para insights");
            insights.Add(new InsightResponse(
                "Estoque indisponível",
                "O assistente não conseguiu ler o microsserviço de estoque. O faturamento continua operacional para consulta de notas.",
                "danger"));
        }

        return insights;
    }

    public async Task<AssistantResponse> AskAsync(string question, CancellationToken ct)
    {
        var q = (question ?? string.Empty).Trim().ToLowerInvariant();
        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Items).ToListAsync(ct);
        var highlights = new List<string>();

        if (q.Contains("aberta") || q.Contains("aberto") || q.Contains("imprim"))
        {
            var open = invoices.Where(i => i.Status == InvoiceStatus.Open).OrderBy(i => i.Number).ToList();
            highlights.AddRange(open.Select(i => $"NF {i.Number:00000} · {i.Items.Count} item(ns)"));
            return new AssistantResponse(
                open.Count == 0
                    ? "Não existem notas Abertas. Crie uma nota para depois imprimir e baixar o estoque."
                    : $"Há {open.Count} nota(s) Aberta(s). A impressão só é permitida nesse status e fecha a nota ao concluir.",
                highlights);
        }

        if (q.Contains("concorr") || q.Contains("saldo 1") || q.Contains("parafuso"))
        {
            return new AssistantResponse(
                "Para demonstrar concorrência, use o produto PAR-M8 (saldo 1) em duas notas Abertas com quantidade 1 e imprima as duas ao mesmo tempo. Uma fecha e baixa o saldo; a outra permanece Aberta com erro de saldo insuficiente.",
                ["PAR-M8 · saldo inicial 1", "Lock pg_advisory_xact_lock no serviço de estoque"]);
        }

        if (q.Contains("falha") || q.Contains("estoque") || q.Contains("chaos") || q.Contains("indispon"))
        {
            return new AssistantResponse(
                "Use Simular falha no painel. O estoque passa a responder HTTP 503. Ao imprimir, o faturamento tenta de novo (Polly), abre o circuit breaker se persistir e devolve feedback. A nota permanece Aberta. Depois desative a falha e imprima novamente — a operação é idempotente.",
                ["Retry + circuit breaker no HttpClient", "Compensação das baixas parciais"]);
        }

        var openCount = invoices.Count(i => i.Status == InvoiceStatus.Open);
        var closedCount = invoices.Count(i => i.Status == InvoiceStatus.Closed);
        return new AssistantResponse(
            $"No momento existem {invoices.Count} nota(s): {openCount} Aberta(s) e {closedCount} Fechada(s). Pergunte sobre impressão, falha de estoque ou concorrência para um roteiro de demonstração.",
            invoices.OrderByDescending(i => i.Number).Take(5).Select(i => $"NF {i.Number:00000} · {i.Status}").ToList());
    }
}
