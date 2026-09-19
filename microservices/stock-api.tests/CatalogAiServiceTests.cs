using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Services;

namespace Korp.Stock.Api.Tests;

public class CatalogAiServiceTests
{
    private readonly CatalogAiService _sut = new();

    [Fact]
    public void Suggest_sem_nome_devolve_texto_generico()
    {
        var result = _sut.Suggest(new SuggestDescriptionRequest("ACO-1", "  "));

        Assert.Contains("Informe o nome", result.Description);
        Assert.Equal("assistente-local", result.Source);
    }

    [Theory]
    [InlineData("Disco de corte", "ferramenta de corte/desbaste")]
    [InlineData("Parafuso M8", "elemento de fixação para montagem")]
    [InlineData("Óleo industrial", "insumo de lubrificação e manutenção")]
    [InlineData("Barra de aço", "matéria-prima metálica para usinagem ou corte")]
    [InlineData("Luva nitrílica", "material de consumo industrial")]
    public void Suggest_classifica_pelo_nome(string name, string category)
    {
        var result = _sut.Suggest(new SuggestDescriptionRequest("X-1", name));

        Assert.Contains(category, result.Description);
        Assert.Contains("X-1", result.Description);
        Assert.Contains(name, result.Description);
    }
}
