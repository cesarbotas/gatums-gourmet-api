using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;

public record ComboItemResponse(Guid ProdutoId, int Quantidade);
public record ProdutoDetailResponse(Guid Id, string Nome, string Descricao, string Categoria, decimal Preco, string FotoUrl, bool Disponivel, string Tipo, IReadOnlyList<ComboItemResponse> Itens);

public interface IGetProdutoByIdHandler
{
    Task<Result<ProdutoDetailResponse>> Handle(Guid id, CancellationToken ct = default);
}

public sealed class GetProdutoByIdHandler(IProdutoRepository produtos) : IGetProdutoByIdHandler
{
    public async Task<Result<ProdutoDetailResponse>> Handle(Guid id, CancellationToken ct = default)
    {
        var p = await produtos.GetByIdAsync(id, ct);
        if (p is null) return Result<ProdutoDetailResponse>.Fail("Produto não encontrado.");

        var itens = p.ComboItens.Select(i => new ComboItemResponse(i.ProdutoId, i.Quantidade)).ToList();
        return Result<ProdutoDetailResponse>.Ok(new ProdutoDetailResponse(p.Id, p.Nome, p.Descricao, p.Categoria, p.Preco, p.FotoUrl, p.Disponivel, p.Tipo.ToString().ToLower(), itens));
    }
}
