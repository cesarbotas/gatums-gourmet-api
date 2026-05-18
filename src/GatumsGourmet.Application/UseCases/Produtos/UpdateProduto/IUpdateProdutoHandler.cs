using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;

public interface IUpdateProdutoHandler
{
    Task<Result<bool>> Handle(UpdateProdutoRequest request, CancellationToken ct = default);
}
