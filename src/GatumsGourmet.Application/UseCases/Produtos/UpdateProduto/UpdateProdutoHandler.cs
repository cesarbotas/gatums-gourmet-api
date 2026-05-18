using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Enums;

namespace GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;

public record UpdateProdutoRequest(Guid Id, string Nome, string Descricao, string Categoria, decimal Preco, string FotoUrl, bool Disponivel);

public interface IUpdateProdutoHandler
{
    Task<Result<bool>> Handle(UpdateProdutoRequest request, CancellationToken ct = default);
}

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
