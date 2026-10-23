using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.DeleteProduto;

public interface IDeleteProdutoHandler
{
    Task<Result<bool>> Handle(Guid id, CancellationToken ct = default);
}
