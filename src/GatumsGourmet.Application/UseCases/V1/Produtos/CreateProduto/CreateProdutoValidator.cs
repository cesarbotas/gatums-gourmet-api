using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.CreateProduto;

public class CreateProdutoValidator : AbstractValidator<CreateProdutoRequest>
{
    public CreateProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoriaId).GreaterThan(0);
        RuleFor(x => x.Preco).GreaterThan(0);
        RuleFor(x => x.FotoUrl).NotEmpty();
    }
}
