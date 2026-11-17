using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;

public class IniciarPixValidator : AbstractValidator<IniciarPixRequest>
{
    public IniciarPixValidator()
    {
        RuleFor(x => x.PedidoId)
            .NotEmpty()
            .WithMessage("PedidoId é obrigatório.");

        When(x => x.Valor.HasValue, () =>
        {
            RuleFor(x => x.Valor!.Value)
                .GreaterThan(0)
                .WithMessage("Valor deve ser maior que zero.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PayerEmail), () =>
        {
            RuleFor(x => x.PayerEmail!)
                .EmailAddress()
                .WithMessage("E-mail do pagador inválido.");
        });
    }
}