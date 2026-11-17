using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;

public class AddCarrinhoItemValidator : AbstractValidator<AddCarrinhoItemRequest>
{
    public AddCarrinhoItemValidator()
    {
        RuleFor(x => x.ProdutoId)
            .NotEmpty().WithMessage("O ID do produto é obrigatório.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
    }
}
