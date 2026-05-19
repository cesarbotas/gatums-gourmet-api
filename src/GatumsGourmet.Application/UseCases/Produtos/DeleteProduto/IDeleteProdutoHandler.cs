using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Produtos.DeleteProduto;

public interface IDeleteProdutoHandler
{
    Task<Result<bool>> Handle(Guid id, CancellationToken ct = default);
}
