using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Carrinho;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;

public record AddCarrinhoItemRequest(Guid ProdutoId, int Quantidade);

public class AddCarrinhoItemValidator : AbstractValidator<AddCarrinhoItemRequest>
{
    public AddCarrinhoItemValidator()
    {
        RuleFor(x => x.ProdutoId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}

public interface IAddCarrinhoItemHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, AddCarrinhoItemRequest request, CancellationToken ct = default);
}

public sealed class AddCarrinhoItemHandler(ICarrinhoRepository carrinho, IProdutoRepository produtos) : IAddCarrinhoItemHandler
{
    public async Task<Result<bool>> Handle(Guid usuarioId, AddCarrinhoItemRequest request, CancellationToken ct = default)
    {
        var produto = await produtos.GetByIdAsync(request.ProdutoId, ct);
        if (produto is null) return Result<bool>.Fail("Produto não encontrado.");

        // Validar estoque: a quantidade total no carrinho + nova não pode exceder o estoque
        var existing = await carrinho.GetItemAsync(usuarioId, request.ProdutoId, ct);
        var quantidadeAtualNoCarrinho = existing?.Quantidade ?? 0;
        var quantidadeTotal = quantidadeAtualNoCarrinho + request.Quantidade;

        if (quantidadeTotal > produto.Estoque)
            return Result<bool>.Fail(
                $"Estoque insuficiente. Disponível: {produto.Estoque}, no carrinho: {quantidadeAtualNoCarrinho}, solicitado: {request.Quantidade}.");

        if (existing is not null)
            existing.Quantidade += request.Quantidade;
        else
            await carrinho.AddAsync(new CarrinhoItem { Id = Guid.NewGuid(), UsuarioId = usuarioId, ProdutoId = produto.Id, Quantidade = request.Quantidade, Nome = produto.Nome, Preco = produto.Preco, FotoUrl = produto.FotoUrl }, ct);

        await carrinho.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

