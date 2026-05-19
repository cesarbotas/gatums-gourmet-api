using FluentValidation;

namespace GatumsGourmet.Application.UseCases.Produtos.CreateProduto;

public class CreateProdutoValidator : AbstractValidator<CreateProdutoRequest>
{
    public CreateProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Categoria).NotEmpty();
        RuleFor(x => x.Preco).GreaterThan(0);
        RuleFor(x => x.FotoUrl).NotEmpty();
    }
}
