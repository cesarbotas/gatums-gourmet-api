using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.UpdateProduto;

public interface IUpdateProdutoHandler
{
    Task<Result<bool>> Handle(UpdateProdutoRequest request, CancellationToken ct = default);
}
