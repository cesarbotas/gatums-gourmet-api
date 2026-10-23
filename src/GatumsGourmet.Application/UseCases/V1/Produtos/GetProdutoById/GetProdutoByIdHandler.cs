using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.GetProdutoById;

public sealed class GetProdutoByIdHandler(IProdutoRepository produtos) : IGetProdutoByIdHandler
{
    public async Task<Result<ProdutoDetailResponse>> Handle(Guid id, CancellationToken ct = default)
    {
        var p = await produtos.GetByIdAsync(id, ct);
        if (p is null) return Result<ProdutoDetailResponse>.Fail("Produto não encontrado.");
        var itens = p.ComboItens.Select(i => new ComboItemResponse(i.ProdutoId, i.Quantidade)).ToList();
        return Result<ProdutoDetailResponse>.Ok(new ProdutoDetailResponse(p.Id, p.Nome, p.Descricao, p.CategoriaId, p.Categoria?.Nome ?? string.Empty, p.Preco, p.FotoUrl, p.Disponivel, p.Tipo.ToString().ToLower(), itens));
    }
}

