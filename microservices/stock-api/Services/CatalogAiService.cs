using Korp.Stock.Api.Contracts;

namespace Korp.Stock.Api.Services;

public class CatalogAiService
{
    public SuggestDescriptionResponse Suggest(SuggestDescriptionRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
        {
            return new SuggestDescriptionResponse(
                "Item industrial para uso em processo produtivo. Informe o nome para gerar uma descrição mais precisa.",
                "assistente-local");
        }

        var category = Classify(name);
        var description =
            $"{name} ({code}) — {category}. Item destinado a operação industrial, com controle de saldo para faturamento e baixa automática na impressão da nota.";

        return new SuggestDescriptionResponse(description, "assistente-local");
    }

    private static string Classify(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("disco") || n.Contains("broca") || n.Contains("ferramenta"))
        {
            return "ferramenta de corte/desbaste";
        }

        if (n.Contains("parafuso") || n.Contains("porca") || n.Contains("arruela"))
        {
            return "elemento de fixação para montagem";
        }

        if (n.Contains("óleo") || n.Contains("graxa") || n.Contains("fluido"))
        {
            return "insumo de lubrificação e manutenção";
        }

        if (n.Contains("aço") || n.Contains("chapa") || n.Contains("barra"))
        {
            return "matéria-prima metálica para usinagem ou corte";
        }

        return "material de consumo industrial";
    }
}
