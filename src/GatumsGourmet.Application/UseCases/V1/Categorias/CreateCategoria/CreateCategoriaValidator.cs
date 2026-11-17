using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;

public class CreateCategoriaValidator : AbstractValidator<CreateCategoriaRequest>
{
    public CreateCategoriaValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Descricao)
            .MaximumLength(500);
    }
}