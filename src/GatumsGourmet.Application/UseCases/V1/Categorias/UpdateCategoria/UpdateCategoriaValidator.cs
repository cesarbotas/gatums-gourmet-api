using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;

public class UpdateCategoriaValidator : AbstractValidator<UpdateCategoriaRequest>
{
    public UpdateCategoriaValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Descricao)
            .MaximumLength(500);
    }
}
