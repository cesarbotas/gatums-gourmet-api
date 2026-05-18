using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Produtos.CreateProduto;

public interface ICreateProdutoHandler
{
    Task<Result<CreateProdutoResponse>> Handle(CreateProdutoRequest request, CancellationToken ct = default);
}
