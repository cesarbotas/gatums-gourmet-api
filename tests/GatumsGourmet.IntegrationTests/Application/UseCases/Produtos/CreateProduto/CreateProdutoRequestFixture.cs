namespace GatumsGourmet.IntegrationTests.Application.UseCases.Produtos.CreateProduto;

/// <summary>
/// Fábrica de payloads válidos para o endpoint de criação de produto.
/// Usa objeto anônimo para refletir exatamente o JSON esperado pela API.
/// </summary>
public static class CreateProdutoRequestFixture
{
    public static object Simples(long categoriaId) => new
    {
        nome = $"Pizza {Guid.NewGuid():N}",
        descricao = "Deliciosa",
        categoriaId,
        preco = 49.90m,
        fotoUrl = "http://img/pizza.png",
        disponivel = true,
        tipo = 0, // Simples
        itens = (object?)null
    };
}
