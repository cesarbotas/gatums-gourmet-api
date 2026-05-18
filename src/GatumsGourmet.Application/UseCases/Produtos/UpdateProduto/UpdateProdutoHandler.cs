using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;

public sealed class UpdateProdutoHandler(IProdutoRepository produtos) : IUpdateProdutoHandler
{
    public async Task<Result<bool>> Handle(UpdateProdutoRequest request, CancellationToken ct = default)
    {
        var produto = await produtos.GetByIdAsync(request.Id, ct);
        if (produto is null) return Result<bool>.Fail("Produto não encontrado.");
        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.Categoria = request.Categoria;
        produto.Preco = request.Preco;
        produto.FotoUrl = request.FotoUrl;
        produto.Disponivel = request.Disponivel;
        await produtos.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
