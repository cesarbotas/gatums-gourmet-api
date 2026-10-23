using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.GetCarrinho;

public record CarrinhoItemResponse(Guid ProdutoId, string Nome, decimal Preco, string FotoUrl, int Quantidade);
public record CarrinhoResponse(IReadOnlyList<CarrinhoItemResponse> Itens, decimal Total);

public interface IGetCarrinhoHandler
{
    Task<Result<CarrinhoResponse>> Handle(Guid usuarioId, CancellationToken ct = default);
}

public sealed class GetCarrinhoHandler(ICarrinhoRepository carrinho) : IGetCarrinhoHandler
{
    public async Task<Result<CarrinhoResponse>> Handle(Guid usuarioId, CancellationToken ct = default)
    {
        var itens = await carrinho.GetByUsuarioAsync(usuarioId, ct);
        var dtos = itens.Select(i => new CarrinhoItemResponse(i.ProdutoId, i.Nome, i.Preco, i.FotoUrl, i.Quantidade)).ToList();
        return Result<CarrinhoResponse>.Ok(new CarrinhoResponse(dtos, dtos.Sum(i => i.Preco * i.Quantidade)));
    }
}

