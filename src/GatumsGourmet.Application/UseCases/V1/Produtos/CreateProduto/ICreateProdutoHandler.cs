using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.CreateProduto;

public interface ICreateProdutoHandler
{
    Task<Result<CreateProdutoResponse>> Handle(CreateProdutoRequest request, CancellationToken ct = default);
}
