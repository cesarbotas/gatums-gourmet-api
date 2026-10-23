using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.DeleteProduto;

public sealed class DeleteProdutoHandler(IProdutoRepository produtos) : IDeleteProdutoHandler
{
    public async Task<Result<bool>> Handle(Guid id, CancellationToken ct = default)
    {
        var produto = await produtos.GetByIdAsync(id, ct);
        if (produto is null) return Result<bool>.Fail("Produto não encontrado.");
        await produtos.DeleteAsync(produto, ct);
        await produtos.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

