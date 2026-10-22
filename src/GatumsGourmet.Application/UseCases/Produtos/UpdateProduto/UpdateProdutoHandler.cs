using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;

public sealed class UpdateProdutoHandler(IProdutoRepository produtos, ICategoriaRepository categorias) : IUpdateProdutoHandler
{
    public async Task<Result<bool>> Handle(UpdateProdutoRequest request, CancellationToken ct = default)
    {
        var produto = await produtos.GetByIdAsync(request.Id, ct);
        if (produto is null) return Result<bool>.Fail("Produto não encontrado.");

        var categoria = await categorias.GetByIdAsync(request.CategoriaId, ct);
        if (categoria is null) return Result<bool>.Fail("Categoria não encontrada.");

        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.CategoriaId = request.CategoriaId;
        produto.Preco = request.Preco;
        produto.FotoUrl = request.FotoUrl;
        produto.Disponivel = request.Disponivel;
        await produtos.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

