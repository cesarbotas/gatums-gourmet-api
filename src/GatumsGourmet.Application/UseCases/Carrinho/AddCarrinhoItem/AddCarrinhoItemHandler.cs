using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities;

namespace GatumsGourmet.Application.UseCases.Carrinho.AddCarrinhoItem;

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

        var existing = await carrinho.GetItemAsync(usuarioId, request.ProdutoId, ct);
        if (existing is not null)
        {
            existing.Quantidade += request.Quantidade;
        }
        else
        {
            await carrinho.AddAsync(new CarrinhoItem
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                ProdutoId = produto.Id,
                Quantidade = request.Quantidade,
                Nome = produto.Nome,
                Preco = produto.Preco,
                FotoUrl = produto.FotoUrl
            }, ct);
        }

        await carrinho.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
